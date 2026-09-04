using UnityEngine;

namespace Houscaper
{
    /// <summary>One pastel building scheme: walls, roof, trim and glazing.</summary>
    public struct Swatch
    {
        public string Name;
        public Color Wall;
        public Color Roof;
        public Color Trim;
        public Color Glass;

        public Swatch(string name, string wall, string roof, string trim, string glass)
        {
            Name = name;
            Wall = Hex(wall);
            Roof = Hex(roof);
            Trim = Hex(trim);
            Glass = Hex(glass);
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }

    public static class Palette
    {
        public static readonly Swatch[] Swatches =
        {
            new Swatch("Cream",  "#f4e9d6", "#d3806f", "#fdf7ec", "#9db9c9"),
            new Swatch("Blush",  "#f1d7d8", "#c1737f", "#fbeeee", "#a3b7c6"),
            new Swatch("Sage",   "#dbe6d3", "#8aa486", "#f0f5eb", "#9fb8bd"),
            new Swatch("Sky",    "#d7e3ef", "#7c98bb", "#eef4fa", "#8fa8bd"),
            new Swatch("Butter", "#f6e5ba", "#d29a57", "#fdf6e2", "#a6bcc4"),
            new Swatch("Lilac",  "#e3dbee", "#8b7eb0", "#f4f0fb", "#9caec4"),
            new Swatch("Clay",   "#ebc6a7", "#b06249", "#f8e6d5", "#9bb2bd"),
            new Swatch("Mint",   "#d3e9e1", "#6da48f", "#eaf7f2", "#94b4b8"),
        };

        /// <summary>Island, water and sky colours the whole scene is tuned around.</summary>
        public static readonly Color Grass      = Swatch.Hex("#c9dcb4");
        public static readonly Color GrassDark  = Swatch.Hex("#a9c295");
        public static readonly Color Cliff      = Swatch.Hex("#cbb9a4");
        public static readonly Color CliffDark  = Swatch.Hex("#a3907c");
        public static readonly Color Water      = Swatch.Hex("#a8cfe0");
        public static readonly Color SkyTop     = Swatch.Hex("#a9d3ef");
        public static readonly Color SkyHorizon = Swatch.Hex("#eaf2f7");
        public static readonly Color SunLight   = Swatch.Hex("#fff0cf");
        public static readonly Color GridLine   = Swatch.Hex("#b6cda2");

        public static Swatch Get(int index)
        {
            if (Swatches.Length == 0) return default;
            return Swatches[((index % Swatches.Length) + Swatches.Length) % Swatches.Length];
        }
    }
}
