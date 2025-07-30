<a name="readme-top"></a>

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">Center Speed</h1>
  <a align="center">A CS2 plugin that shows the player's speed using point_worldtext.</a><br><br>
  <img width="1368" height="900" alt="image" src="https://github.com/user-attachments/assets/67acb264-4142-4d0f-826a-cebf3e4d177f"/>



  <p align="center">
    <br>
    <a href="https://github.com/M-archand/CenterSpeed/releases/tag/1.0.0">Download</a>
    <br><br>
    <a href="https://www.youtube.com/watch?v=w43JOy6iPXs">Demo (YouTube)</a>
  </p>
</div>

<!-- ABOUT THE PROJECT -->

### Dependencies

To use this plugin, you'll need the following dependencies installed:

- [**CounterStrikeSharp (Required)**](https://github.com/roflmuffin/CounterStrikeSharp): CounterStrikeSharp allows you to write server plugins in C# for Counter-Strike 2.
- [**CS2-GameHUD (Required)**](https://github.com/darkerz7/CS2-GameHUD): A shared API used to create the world text.
- [**Clientprefs (Optional)**](https://github.com/Cruze03/Clientprefs): A shared API that allows you to save the on/off cookie for each player to a database to have persistence between server restarts. Will automatically be used if it is installed, otherwise players will have to enable it again if the server restarts. If using CS2MenuManager, it will also save the other text settings here.
- [**CS2MenuManager (Optional)**](https://github.com/schwarper/CS2MenuManager): A shared API used to manage the settings menu for the player. If not installed, the menu command won't be available and the player will use the settings you have defined in the config.

<!-- CONFIG -->

## Configuration

- A config file will be generated on first use located in _/addons/counterstrikesharp/configs/CenterSpeed_
Example:
```json
{
  "TextSettings": {
    "Size": 45,
    "Font": "Arial",
    "Color": "Salmon", # System.Drawing.Colors: https://i.sstatic.net/lsuz4.png
    "Position": 0 # Y position of the text. 0 = middle, negative int like -40 is lower, positive int like 40 is higher
  },
  "Command": "cs,centerspeed", # The command(s) to enable/disable centerspeed (!cs)
  "TickInterval": 4, # How often the hud will update. 64 ticks = 1 second.
  "DisableCrosshair": true, # Whether to disable the knife crosshair for the player while centerspeed is enabled
  "Use2DSpeed": true, # true = 2D speed = X, Y speed. false = 3D speed = X,Y,Z speed
  "MenuType": "WasdMenu", # The CS2MenuManager menu to be used for changing settings. 
  "Channel": 17, # This is for the CS2-GameHUDAPI, if you don't know what it does then leave it as is
  "ConfigVersion": 1
}

```
<!-- CONFIG -->

## Commands
- !cs - Toggles centerspeed
- !cs menu - Opens the settings menu (if CS2MenuManager is installed)
- !cs is customizable in the config
<!-- ROADMAP -->

## Roadmap

- [ ] Add Font to player settings
- [ ] Translations
- [ ] Non OnTick based updates

<!-- LICENSE -->

## License

Distributed under the GPL-3.0 License. See `LICENSE.md` for more information.

![GitHub Downloads](https://img.shields.io/github/downloads/M-archand/CenterSpeed/total?style=for-the-badge)

<p align="right">(<a href="#readme-top">back to top</a>)</p>
