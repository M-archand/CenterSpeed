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
	public class CenterSpeed : BasePlugin
	{
		public override string ModuleName => "CenterSpeed";
		public override string ModuleAuthor => "Marchand";
		public override string ModuleVersion => "1.0.0";

		static IGameHUDAPI? _api;
        private bool use2DSpeed = true;
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
                Logger.LogError("CS2-GameHUDAPI failed! Did you make sure to install it?");
            }

            RegisterListener<Listeners.OnTick>(PlayerOnTick);
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnTick>(PlayerOnTick);
        }

        private void PlayerOnTick()
        {
            if (_api == null) return;

            foreach (int slot in _speedHudSlots)
            {
                var player = Utilities.GetPlayerFromSlot(slot);
                if (player == null || !player.IsValid) continue;

                Vector playerSpeed = player.PlayerPawn!.Value!.AbsVelocity;
                string formattedPlayerVel = Math.Round(
                    use2DSpeed
                        ? playerSpeed.Length2D()
                        : playerSpeed.Length()
                ).ToString("0000");

                _api.Native_GameHUD_ShowPermanent(player, 0, formattedPlayerVel);
            }
        }

		[ConsoleCommand("css_cs", "Print the player's speed to the center of their screen.")]
		[CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
		public void OnCommandTest(CCSPlayerController? player, CommandInfo command)
        {
            if (_api == null || player == null || !player.IsValid) return;

            int slot = player.Slot;
            byte channel = 0;

            if (_speedHudSlots.Contains(slot)) // Turn off
            {
                _api.Native_GameHUD_Remove(player, channel);
                _speedHudSlots.Remove(slot);
            }
            else // Turn on
            {
                Vector velocity = player.PlayerPawn!.Value!.AbsVelocity;
                string initialVel = Math.Round(use2DSpeed ? velocity.Length2D() : velocity.Length()).ToString("0000");

                var position = new Vector(-4, 0, 70);
                var color = Color.Salmon;
                int fontSize = 16;
                string fontName = "Verdana";
                float units = 0.25f;

                _api.Native_GameHUD_SetParams(player, channel, position, color, fontSize, fontName, units);
                _api.Native_GameHUD_ShowPermanent(player, channel, initialVel);
                _speedHudSlots.Add(slot);
            }
        }
	}
}