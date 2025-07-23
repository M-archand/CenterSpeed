<a name="readme-top"></a>

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">Center Speed</h1>
  <a align="center">A CS2 plugin that shows the player's speed using point_worldtext.</a><br><br>
  <img width="2316" height="1303" alt="image" src="https://github.com/user-attachments/assets/21325302-c076-48da-a151-afa2074dfc50" />


  <p align="center">
    <br />
    <a href="https://github.com/M-archand/CenterSpeed/releases/tag/1.0.0">Download</a>
  </p>
</div>

<!-- ABOUT THE PROJECT -->

### Dependencies

To use this plugin, you'll need the following dependencies installed:

- [**CounterStrikeSharp (Required)**](https://github.com/roflmuffin/CounterStrikeSharp): CounterStrikeSharp allows you to write server plugins in C# for Counter-Strike 2.
- [**CS2-GameHUD (Required)**](https://github.com/darkerz7/CS2-GameHUD): A shared API used to create the world text.
- [**Clientprefs (Optional)**](https://github.com/Cruze03/Clientprefs): This allows you to save the enable/disable cookie for the player to a database to have persistence between server restarts. Will automatically be used if it is installed, otherwise players will have to enable it again if the server restarts.

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
  "Channel": 17, # This is for the CS2-GameHUDAPI, if you don't know what it does then leave it as is
  "ConfigVersion": 1
}

```

<!-- ROADMAP -->

## Roadmap

- [ ] ???

<!-- LICENSE -->

## License

Distributed under the GPL-3.0 License. See `LICENSE.md` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
