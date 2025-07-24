using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace CenterSpeed
{
    public class PluginConfig : BasePluginConfig
    {   
        /////////////////////////////////////////////////////////////////////////////////

        [JsonPropertyName("TextSettings")]
        public TextSettings TextSettings { get; set; } = new TextSettings();

        /////////////////////////////////////////////////////////////////////////////////
        
        [JsonPropertyName("Command")]
        public string Command { get; set; } = "cs,centerspeed";

        [JsonPropertyName("TickInterval")]
        public int TickInterval { get; set; } = 4;

        [JsonPropertyName("DisableCrosshair")]
        public bool DisableCrosshair { get; set; } = true;

        [JsonPropertyName("Use2DSpeed")]
        public bool Use2DSpeed { get; set; } = true;

        [JsonPropertyName("MenuType")]
        public string MenuType { get; set; } = "WasdMenu";

        [JsonPropertyName("Channel")]
        public byte Channel { get; set; } = 7;

        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 1;
    }

    public sealed class TextSettings
    {
        [JsonPropertyName("Size")]
        public int Size { get; set; } = 40;

        [JsonPropertyName("Font")]
        public string Font { get; set; } = "Arial";

        [JsonPropertyName("Color")]
        public string Color { get; set; } = "White";

        [JsonPropertyName("Position")]
        public float Position { get; set; } = 0F;

    }
}