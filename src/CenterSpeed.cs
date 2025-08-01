using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using CS2_GameHUDAPI;
using Clientprefs.API;
using CS2MenuManager.API.Class;
using System.Drawing;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CenterSpeed
{
    public partial class Plugin : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "CenterSpeed";
        public override string ModuleAuthor => "Marchand";
        public override string ModuleVersion => "1.0.0";

        public required PluginConfig Config { get; set; } = new PluginConfig();
        static IGameHUDAPI? _api;
        private IClientprefsApi? _prefs;
        private readonly HashSet<ulong> _enabledSteamIDs = new();
        private int _configCookieId = -1;
        private bool _hasMenuManager;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            // Check for CS2-GameHUDAPI installation
            try
            {
                PluginCapability<IGameHUDAPI> CapabilityCP = new("gamehud:api");
                _api = IGameHUDAPI.Capability.Get();
            }
            catch (Exception)
            {
                _api = null;
                Logger.LogInformation("CS2-GameHUDAPI failed to load! Did you make sure to install it?");
            }

            // Check for Clientprefs installation
            var prefsCap = new PluginCapability<IClientprefsApi>("Clientprefs");
            try
            {
                _prefs = prefsCap.Get();
            }
            catch
            {
                _prefs = null;
                Server.PrintToConsole("[CenterSpeed] Clientprefs not found! CenterSpeed on/off will be reset whenever the server restarts.");
            }

            if (_prefs != null) // Hook Clientprefs events
            {
                _prefs.OnDatabaseLoaded += OnClientprefsDatabaseReady;
                _prefs.OnPlayerCookiesCached += OnPlayerPreferencesLoaded;
            }

            // Check for CS2MenuManager installation
            try
            {
                var dummy = MenuManager.MenuTypesList;
                _hasMenuManager = true;
            }
            catch (Exception)
            {
                _hasMenuManager = false;
                Server.PrintToConsole("[CenterSpeed] CS2MenuManager API not found! CenterSpeed menu command has been disabled.");
            }

            RegisterListener<Listeners.OnTick>(PlayerOnTick);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerReady);
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;

            var aliases = Config.Command.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim());
            foreach (var alias in aliases)
                AddCommand($"css_{alias}", "Toggle CenterSpeed or open its menu", OnAliasCommand);

            if (Config.Channel > 32)
            {
                Logger.LogWarning($"Configured Channel {Config.Channel} is out of range (0-32). Falling back to channel 7.");
                Config.Channel = 7;
            }

            if (config.Version < Config.Version)
                Logger.LogWarning($"Configuration version mismatch (Expected: {0} | Current: {1})", Config.Version, config.Version);

            // Validate Config.MenuType
            if (!MenuManager.MenuTypesList.ContainsKey(Config.MenuType))
            {
                Logger.LogWarning($"Invalid MenuType `{Config.MenuType}`, defaulting to “ScreenMenu”");
                Config.MenuType = "WasdMenu";
            }
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnTick>(PlayerOnTick);
        }

        private void OnPlayerPreferencesLoaded(CCSPlayerController player)
        {
            if (_configCookieId < 0 || _prefs == null)
                return;

            var settings = LoadSettings(player);
            if (settings.Enabled)
            {
                ApplyCenterSpeedHud(player);

                bool hide = (_prefs != null && _configCookieId >= 0) ? settings.DisableCrosshair : Config.DisableCrosshair;
                if (hide)
                    player.ReplicateConVar("weapon_reticle_knife_show", "false");
            }
            if (!IsCenterSpeedEnabled(player))
                WarmUpHud(player);
        }

        private void OnClientprefsDatabaseReady()
        {
            _configCookieId = _prefs!.RegPlayerCookie("centerspeed_config", "JSON CenterSpeed settings", CookieAccess.CookieAccess_Public);
        }

        private CenterSpeedSettings LoadSettings(CCSPlayerController player)
        {
            string raw = _prefs!.GetPlayerCookie(player, _configCookieId) ?? "{}";
            CenterSpeedSettings settings;
            try
            {
                settings = JsonSerializer.Deserialize<CenterSpeedSettings>(raw) ?? new CenterSpeedSettings();
            }
            catch
            {
                settings = new CenterSpeedSettings();
            }
            return settings;
        }

        private void SaveSettings(CCSPlayerController player, CenterSpeedSettings s)
        {
            string raw = JsonSerializer.Serialize(s);
            _prefs!.SetPlayerCookie(player, _configCookieId, raw);
        }

        private bool IsCenterSpeedEnabled(CCSPlayerController player)
        {
            if (!player.IsValid)
                return false;

            if (_prefs != null && _configCookieId >= 0 && !_prefs.ArePlayerCookiesCached(player))
            {
                try
                {
                    string raw = _prefs.GetPlayerCookie(player, _configCookieId);
                    if (!string.IsNullOrEmpty(raw))
                    {
                        var settings = JsonSerializer.Deserialize<CenterSpeedSettings>(raw) ?? new CenterSpeedSettings();
                        return settings.Enabled;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"CenterSpeed: failed to read Clientprefs cookie for {player.PlayerName} ({player.SteamID}): {ex.Message}");
                }
            }

            return _enabledSteamIDs.Contains(player.SteamID);
        }

        private void PlayerOnTick()
        {
            if (_api == null) return;

            // Only update every X ticks
            int interval = Math.Max(1, Config.TickInterval);
            if (Server.TickCount % interval != 0)
                return;

            foreach (var player in Utilities.GetPlayers().Where(IsCenterSpeedEnabled))
            {
                var pawnHandle = player.PlayerPawn;
                if (pawnHandle == null || !pawnHandle.IsValid || pawnHandle.Value == null) continue;

                Vector playerSpeed = player.PlayerPawn!.Value!.AbsVelocity;
                string formatted = Math.Round(Config.Use2DSpeed ? playerSpeed.Length2D() : playerSpeed.Length()).ToString("0000");
                _api.Native_GameHUD_ShowPermanent(player, Config.Channel, formatted);
            }
        }

        public void OnAliasCommand(CCSPlayerController? player, CommandInfo cmd)
        {
            if (_api == null || player == null || !player.IsValid)
                return;

            // If the player typed “css_{alias} menu”
            if (cmd.ArgCount >= 1 && cmd.GetArg(1).Equals("menu", StringComparison.OrdinalIgnoreCase))
            {
                if (_hasMenuManager)
                    OnSpeedMenuCommand(player, cmd);
                else
                    player.PrintToChat("The CenterSpeed settings menu hasn't been enabled.");
            }
            else // "css_{alias}" with no args
            {
                OnCenterSpeedCommand(player, cmd);
            }
        }

        public void OnCenterSpeedCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (_api == null || player == null || !player.IsValid) return;

            byte channel = Config.Channel;
            bool currentlyEnabled = IsCenterSpeedEnabled(player);
            bool newEnabled = !currentlyEnabled;

            if (_prefs != null && _configCookieId >= 0)
            {
                var settings = LoadSettings(player);
                settings.Enabled = newEnabled;
                SaveSettings(player, settings);
            }
            else
            {
                if (newEnabled)
                    _enabledSteamIDs.Add(player.SteamID);
                else
                    _enabledSteamIDs.Remove(player.SteamID);
            }

            if (newEnabled)
                ApplyCenterSpeedHud(player);
            else
                _api.Native_GameHUD_Remove(player, channel);

            var s = (_prefs != null && _configCookieId >= 0)
                ? LoadSettings(player)
                : new CenterSpeedSettings { DisableCrosshair = Config.DisableCrosshair };

            bool hide = s.DisableCrosshair;
            player.ReplicateConVar("weapon_reticle_knife_show", newEnabled && hide ? "false" : "true");

            var color = newEnabled ? ChatColors.Lime : ChatColors.LightRed;
            var word = newEnabled ? "ENABLED" : "DISABLED";
            player.PrintToChat($"CenterSpeed: {color}{word}");
        }

        private void ApplyCenterSpeedHud(CCSPlayerController player)
        {
            var settings = (_prefs != null && _configCookieId >= 0)
                ? LoadSettings(player)
                : new CenterSpeedSettings { Enabled = true };

            Vector playerSpeed = player.PlayerPawn!.Value!.AbsVelocity;
            string vel = Math.Round(Config.Use2DSpeed ? playerSpeed.Length2D() : playerSpeed.Length()).ToString("0000");

            float height = settings.Position ?? Config.TextSettings.Position;
            var position = new Vector(0.0F, height, 7.0F);
            string c = string.IsNullOrEmpty(settings.Color) ? Config.TextSettings.Color : settings.Color;
            Color color = Color.FromName(c);
            int size = settings.Size ?? Config.TextSettings.Size;
            string font = string.IsNullOrEmpty(settings.Font) ? Config.TextSettings.Font : settings.Font;
            float scale = size / 7000.0F;
            var justifyH = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
            var justifyV = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;

            _api!.Native_GameHUD_SetParams(player, Config.Channel, position, color, size, font, scale, justifyH, justifyV);
            _api.Native_GameHUD_ShowPermanent(player, Config.Channel, vel);

            bool hide = (_prefs != null && _configCookieId >= 0) ? settings.DisableCrosshair : Config.DisableCrosshair;
            player.ReplicateConVar("weapon_reticle_knife_show", hide ? "false" : "true");
        }

        private void WarmUpHud(CCSPlayerController player)
        {
            if (_api == null || !player.IsValid) return;

            var settings = (_prefs != null && _configCookieId >= 0)
                ? LoadSettings(player)
                : new CenterSpeedSettings { Enabled = true };

            float height = settings.Position ?? Config.TextSettings.Position;
            var position = new Vector(0.0F, height, 7.0F);
            string c = string.IsNullOrEmpty(settings.Color) ? Config.TextSettings.Color : settings.Color;
            Color color = Color.FromName(c);
            int size = settings.Size ?? Config.TextSettings.Size;
            string font = string.IsNullOrEmpty(settings.Font) ? Config.TextSettings.Font : settings.Font;
            float scale = size / 7000.0F;
            var justifyH = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
            var justifyV = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;

            _api!.Native_GameHUD_SetParams(player, Config.Channel, position, color, size, font, scale, justifyH, justifyV);
            _api.Native_GameHUD_ShowPermanent(player, Config.Channel, "");
            _api.Native_GameHUD_Remove(player, Config.Channel);
        }

        [GameEventHandler(mode: HookMode.Post)]
        private HookResult OnPlayerReady(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid!;
            if (!player.IsValid)
                return HookResult.Continue;

            if (IsCenterSpeedEnabled(player))
            {
                ApplyCenterSpeedHud(player);
                if (Config.DisableCrosshair)
                    player.ReplicateConVar("weapon_reticle_knife_show", "false");
            }

            return HookResult.Continue;
        }

        [GameEventHandler(mode: HookMode.Post)]
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && IsCenterSpeedEnabled(p)))
            {
                Server.NextFrame(() =>
                {
                    ApplyCenterSpeedHud(p);
                    if (Config.DisableCrosshair)
                        p.ReplicateConVar("weapon_reticle_knife_show", "false");
                });
            }
            return HookResult.Continue;
        }

        private class CenterSpeedSettings
        {
            public bool Enabled { get; set; } = false;
            public string Color { get; set; } = "";
            public int? Size { get; set; } = null;
            public string Font { get; set; } = "";
            public float? Position { get; set; } = null;
            public bool DisableCrosshair { get; set; } = true;
        }
    }
}