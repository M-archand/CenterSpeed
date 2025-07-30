using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Interface;

namespace CenterSpeed
{
    public partial class Plugin : BasePlugin, IPluginConfig<PluginConfig>
    {
        private void OnSpeedMenuCommand(CCSPlayerController? player, CommandInfo cmd)
        {
            if (player == null || !player.IsValid) return;

            if (!IsCenterSpeedEnabled(player))
                OnCenterSpeedCommand(player, cmd);

            // Create the menu as defined in the config
            var main = MenuManager.MenuByType(Config.MenuType, "CenterSpeed Settings", this);

            // Three top-level options
            main.AddItem("Color",    (p, opt) => OpenColorMenu(p, main));
            main.AddItem("Size",     (p, opt) => OpenSizeMenu(p, main));
            main.AddItem("Font",     (p, opt) => OpenFontMenu(p, main));
            main.AddItem("Position", (p, opt) => OpenPositionMenu(p, main));
            main.AddItem("Crosshair",(p, opt) => OpenCrosshairMenu(p, main));

            // Show it until they exit
            main.Display(player, 0);
        }

        private void OpenColorMenu(CCSPlayerController player, IMenu parent)
        {
            var menu = MenuManager.MenuByType(Config.MenuType, "Choose Color", this);
            menu.PrevMenu = parent;

            foreach (var (label, colorName) in _colors)
            {
                var opt = menu.AddItem(label, (p, _) =>
                {
                    var settings = LoadSettings(p);
                    settings.Color = colorName;
                    SaveSettings(p, settings);
                    ApplyCenterSpeedHud(p);
                });

                opt.PostSelectAction = PostSelectAction.Nothing;
            }

            menu.Display(player, 0);
        }

        private void OpenSizeMenu(CCSPlayerController player, IMenu parent)
        {
            var menu = MenuManager.MenuByType(Config.MenuType, "Choose Size", this);
            menu.PrevMenu = parent;

            // Small/Medium/Large default options
            foreach (var (label, sz) in _sizes)
            {
                var opt = menu.AddItem(label, (p, _) =>
                {
                    var settings = LoadSettings(p);
                    settings.Size = sz;
                    SaveSettings(p, settings);
                    ApplyCenterSpeedHud(p);
                });

                opt.PostSelectAction = PostSelectAction.Nothing;
            }

            // Increase size +1
            var inc = menu.AddItem("Increase", (p, _) =>
            {
                var settings = LoadSettings(p);
                int current = settings.Size ?? Config.TextSettings.Size;
                settings.Size = current + 1;
                SaveSettings(p, settings);
                ApplyCenterSpeedHud(p);
            });

            // Decrease size -1
            var dec = menu.AddItem("Decrease", (p, _) =>
            {
                var settings = LoadSettings(p);
                int current = settings.Size ?? Config.TextSettings.Size;
                settings.Size = current - 1;
                SaveSettings(p, settings);
                ApplyCenterSpeedHud(p);
            });

            dec.PostSelectAction = PostSelectAction.Nothing;
            inc.PostSelectAction = PostSelectAction.Nothing;

            menu.Display(player, 0);
        }

        private void OpenFontMenu(CCSPlayerController player, IMenu parent)
        {
            var menu = MenuManager.MenuByType(Config.MenuType, "Choose Font", this);
            menu.PrevMenu = parent;

            foreach (var fontName in _fonts)
            {
                var opt = menu.AddItem(fontName, (p, _) =>
                {
                    var settings = LoadSettings(p);
                    settings.Font = fontName;
                    SaveSettings(p, settings);
                    ApplyCenterSpeedHud(p);
                });

                opt.PostSelectAction = PostSelectAction.Nothing;
            }

            menu.Display(player, 0);
        }

        private void OpenPositionMenu(CCSPlayerController player, IMenu parent)
        {
            var menu = MenuManager.MenuByType(Config.MenuType, "Choose Position", this);
            menu.PrevMenu = parent;

            // Top/Middle/Bottom default options
            foreach (var (label, pos) in _positions)
            {
                var opt = menu.AddItem(label, (p, _) =>
                {
                    var settings = LoadSettings(p);
                    settings.Position = pos;
                    SaveSettings(p, settings);
                    ApplyCenterSpeedHud(p);
                });

                opt.PostSelectAction = PostSelectAction.Nothing;
            }

            // Move Up +0.2f
            var up = menu.AddItem("Up", (p, _) =>
            {
                var settings = LoadSettings(p);
                float current = settings.Position ?? Config.TextSettings.Position;
                settings.Position = current + 0.2f;
                SaveSettings(p, settings);
                ApplyCenterSpeedHud(p);
            });

            // Move Down -0.2f
            var down = menu.AddItem("Down", (p, _) =>
            {
                var settings = LoadSettings(p);
                float current = settings.Position ?? Config.TextSettings.Position;
                settings.Position = current - 0.2f;
                SaveSettings(p, settings);
                ApplyCenterSpeedHud(p);
            });

            up.PostSelectAction = PostSelectAction.Nothing;
            down.PostSelectAction = PostSelectAction.Nothing;

            menu.Display(player, 0);
        }

        private void OpenCrosshairMenu(CCSPlayerController player, IMenu parent)
        {
            var menu = MenuManager.MenuByType(Config.MenuType, "Knife Crosshair", this);
            menu.PrevMenu = parent;

            var disable = menu.AddItem("Hide Crosshair", (p, _) =>
            {
                var settings = LoadSettings(p);
                settings.DisableCrosshair = true;
                SaveSettings(p, settings);
                ApplyCenterSpeedHud(p);
            });

            var enable  = menu.AddItem("Show Crosshair", (p, _) =>
            {
                var settings = LoadSettings(p);
                settings.DisableCrosshair = false;
                SaveSettings(p, settings);
                ApplyCenterSpeedHud(p);
            });

            disable.PostSelectAction = PostSelectAction.Nothing;
            enable.PostSelectAction  = PostSelectAction.Nothing;

            menu.Display(player, 0);
        }

        private readonly (string name, string value)[] _colors =
        [
            ("White",       "White"),
            ("Grey",        "Silver"),
            ("Lime",        "Lime"),
            ("Green",       "Green"),
            ("Light Red",   "Tomato"),
            ("Red",         "Red"),
            ("Dark Red",    "DarkRed"),
            ("Yellow",      "Yellow"),
            ("Orange",      "OrangeRed"),
            ("Cyan",        "Cyan"),
            ("Light Blue",  "DodgerBlue"),
            ("Dark Blue",   "Navy"),
            ("Pink",        "Salmon"),
            ("Violet",      "Violet"),
            ("Magenta",     "Magenta"),
        ];
        
        private readonly (string label, int size)[] _sizes =
        [
            ("Small",  40),
            ("Medium", 46),
            ("Large",  52)
        ];

        private readonly string[] _fonts =
        [
            "Arial",
            "Arial Black",
            "Arial Bold",
            "Arial Narrow",
            "Arial Unicode MS",
            "Comic Sans MS",
            "Courier New",
            "HalfLife2",
            "Lucida Console",
            "MS Shell Dlg 2",
            "Marlett",
            "Stratum2",
            "Tahoma",
            "Times New Roman",
            "Trebuchet MS",
            "Verdana"
        ];

        private readonly (string label, float pos)[] _positions =
        [
            ("Top",     3.2f),
            ("Middle",  0.0f),
            ("Bottom", -3.25f)
        ];
    }
}