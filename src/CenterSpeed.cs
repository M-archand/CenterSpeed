using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using CS2_GameHUDAPI;
using Clientprefs.API;
using System.Drawing;
using Microsoft.Extensions.Logging;
using System.Reflection;

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

        private int _centerSpeedCookieId = -1;

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
                Logger.LogError("CS2-GameHUDAPI failed to load! Did you make sure to install it?");
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
            }

            if (_prefs != null) // Hook Clientprefs events
            {
                _prefs.OnDatabaseLoaded += OnClientprefsDatabaseReady;
                _prefs.OnPlayerCookiesCached += OnPlayerPreferencesLoaded;
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
                AddCommand($"css_{alias}", "The command used to show the center speed", OnCenterSpeedCommand);

            if (Config.Channel > 32)
            {
                Logger.LogError($"Configured Channel {Config.Channel} is out of range (0-32). Falling back to channel 7.");
                Config.Channel = 7;
            }

            if (config.Version < Config.Version)
                Logger.LogError($"Configuration version mismatch (Expected: {0} | Current: {1})", Config.Version, config.Version);
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnTick>(PlayerOnTick);
        }

        private void OnPlayerPreferencesLoaded(CCSPlayerController player)
        {
            if (_centerSpeedCookieId < 0)
                return;

            var val = _prefs!.GetPlayerCookie(player, _centerSpeedCookieId);
            if (val.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                ApplyCenterSpeedHud(player);
                if (Config.DisableCrosshair)
                    player.ReplicateConVar("weapon_reticle_knife_show", "false");
            }
        }
        
        private void OnClientprefsDatabaseReady()
        {
            _centerSpeedCookieId = _prefs!.RegPlayerCookie("centerspeed_enabled","Whether CenterSpeed is enabled", CookieAccess.CookieAccess_Public);
        }
        
        private bool IsCenterSpeedEnabled(CCSPlayerController player)
        {
            if (!player.IsValid) 
                return false;

            if (_prefs != null && _centerSpeedCookieId >= 0)
            {
                return _prefs
                    .GetPlayerCookie(player, _centerSpeedCookieId)
                    .Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // Fallback to in-memory SteamID tracking if no Clientprefs
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

        public void OnCenterSpeedCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (_api == null || player == null || !player.IsValid) return;

            byte channel = Config.Channel;

            bool enabled = IsCenterSpeedEnabled(player); // Turn centerspeed off
            if (enabled)
            {
                _api.Native_GameHUD_Remove(player, channel);

                if (_prefs != null && _centerSpeedCookieId >= 0)
                    _prefs.SetPlayerCookie(player, _centerSpeedCookieId, "false");
                else
                    _enabledSteamIDs.Remove(player.SteamID);

                if (Config.DisableCrosshair)
                    player.ReplicateConVar("weapon_reticle_knife_show", "true");

                player.PrintToChat($"CenterSpeed: {ChatColors.LightRed}DISABLED");
            }
            else // Turn centerspeed on
            {
                ApplyCenterSpeedHud(player);

                if (_prefs != null && _centerSpeedCookieId >= 0)
                {
                    _prefs.SetPlayerCookie(player, _centerSpeedCookieId, "true");

                    var pluginField = _prefs.GetType()
                        .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(f => f.FieldType.Name == "Clientprefs");
                    if (pluginField != null)
                    {
                        var pluginInst = pluginField.GetValue(_prefs)!;
                        var saveMethod = pluginInst.GetType()
                            .GetMethod("SavePlayerCookies",
                                    BindingFlags.NonPublic | BindingFlags.Instance,
                                    null,
                                    new[]{ typeof(string) },
                                    null);
                        saveMethod?.Invoke(pluginInst, new object[]{ player.SteamID.ToString() });
                    }
                }
                else
                {
                    _enabledSteamIDs.Add(player.SteamID);
                }

                if (Config.DisableCrosshair)
                    player.ReplicateConVar("weapon_reticle_knife_show", "false");

                player.PrintToChat($"CenterSpeed: {ChatColors.Lime}ENABLED");
            }
        }

        private void ApplyCenterSpeedHud(CCSPlayerController player)
        {
            Vector playerSpeed = player.PlayerPawn!.Value!.AbsVelocity;
            string vel = Math.Round(Config.Use2DSpeed ? playerSpeed.Length2D() : playerSpeed.Length()).ToString("0000");

            float height  = Config.TextSettings.Position;
            var position  = new Vector(0.0F, height, 7.0F);
            Color color   = Color.FromName(Config.TextSettings.Color);
            int size      = Config.TextSettings.Size;
            string font   = Config.TextSettings.Font;
            float scale   = size / 7000.0F;
            var justifyH  = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
            var justifyV  = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;

            _api!.Native_GameHUD_SetParams(player, Config.Channel, position, color, size, font, scale, justifyH, justifyV);
            _api.Native_GameHUD_ShowPermanent(player, Config.Channel, vel);
            
            if (Config.DisableCrosshair)
                player.ReplicateConVar("weapon_reticle_knife_show", "false");
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
    }
}