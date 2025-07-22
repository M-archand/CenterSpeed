using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using CS2_GameHUDAPI;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace CenterSpeed
{
    public partial class Plugin : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "CenterSpeed";
        public override string ModuleAuthor => "Marchand";
        public override string ModuleVersion => "1.0.0";

        public required PluginConfig Config { get; set; } = new PluginConfig();
        static IGameHUDAPI? _api;
        private readonly HashSet<int> _speedHudSlots = new();

        public override void OnAllPluginsLoaded(bool hotReload)
        {
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
                Logger.LogError($"Configured Channel {Config.Channel} is out of range (0-32). Falling back to channel 17.");
                Config.Channel = 17;
            }
            
            if (config.Version < Config.Version)
                Logger.LogError($"Configuration version mismatch (Expected: {0} | Current: {1})", Config.Version, config.Version);
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnTick>(PlayerOnTick);
        }

        private void PlayerOnTick()
        {
            if (_api == null) return;

            // Only update every X ticks
            int interval = Config.TickInterval;
            if (interval < 1) interval = 1;
            if (Server.TickCount % interval != 0)
                return;

            foreach (int slot in _speedHudSlots)
            {
                var player = Utilities.GetPlayerFromSlot(slot);
                if (player == null || !player.IsValid) 
                    continue;

                var pawnHandle = player.PlayerPawn;
                if (pawnHandle == null || !pawnHandle.IsValid || pawnHandle.Value == null)
                    continue;

                Vector playerSpeed = pawnHandle.Value.AbsVelocity;
                string formatted = Math.Round(Config.Use2DSpeed ? playerSpeed.Length2D() : playerSpeed.Length()).ToString("0000");

                _api.Native_GameHUD_ShowPermanent(player, Config.Channel, formatted);
            }
        }

        public void OnCenterSpeedCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (_api == null || player == null || !player.IsValid) return;

            int slot = player.Slot;
            byte channel = Config.Channel;

            if (_speedHudSlots.Contains(slot)) // Turn centerspeed off
            {
                _api.Native_GameHUD_Remove(player, channel);
                _speedHudSlots.Remove(slot);

                if (Config.DisableCrosshair)
                    player.ReplicateConVar("weapon_reticle_knife_show", "true");

                player.PrintToChat($"CenterSpeed: {ChatColors.LightRed}DISABLED");
            }
            else // Turn centerspeed on
            {
                ApplyCenterSpeedHud(player);
                _speedHudSlots.Add(slot);

                if (Config.DisableCrosshair)
                    player.ReplicateConVar("weapon_reticle_knife_show", "false");

                player.PrintToChat($"CenterSpeed: {ChatColors.Lime}ENABLED");
            }
        }

        private void ApplyCenterSpeedHud(CCSPlayerController player)
        {
            Vector v = player.PlayerPawn!.Value!.AbsVelocity;
            string vel = Math.Round(Config.Use2DSpeed ? v.Length2D() : v.Length()).ToString("0000");

            float height  = Config.TextSettings.Position;
            var position  = new Vector(0.0F, height, 7.0F);
            Color color   = Color.FromName(Config.TextSettings.Color);
            int size      = Config.TextSettings.Size;
            string font   = Config.TextSettings.Font;
            float scale = size / 7000.0F;
            var justifyH  = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
            var justifyV  = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;

            _api!.Native_GameHUD_SetParams(player, Config.Channel, position, color, size, font, scale, justifyH, justifyV);
            _api.Native_GameHUD_ShowPermanent(player, Config.Channel, vel);
        }
        
        [GameEventHandler(mode: HookMode.Post)]
        private HookResult OnPlayerReady(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid!;

            if (_speedHudSlots.Contains(player.Slot))
                ApplyCenterSpeedHud(player);
            return HookResult.Continue;
        }

        [GameEventHandler(mode: HookMode.Post)]
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid))
            {
                if (_speedHudSlots.Contains(p.Slot))
                    Server.NextFrame(() => ApplyCenterSpeedHud(p));
            }
            return HookResult.Continue;
        }
    }
}