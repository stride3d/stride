// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Mathematics;

/// <summary>
/// List of predefined <see cref="Color" />.
/// </summary>
public partial struct Color
{
    /// <summary>
    /// Zero color.
    /// </summary>
    public static readonly Color Zero = FromBgra(0x00000000);

    /// <summary>
    /// Transparent color.
    /// </summary>
    public static readonly Color Transparent = FromBgra(0x00000000);

    /// <summary>
    /// AliceBlue color.
    /// </summary>
    public static readonly Color AliceBlue = FromBgra(0xFFF0F8FF);

    /// <summary>
    /// AntiqueWhite color.
    /// </summary>
    public static readonly Color AntiqueWhite = FromBgra(0xFFFAEBD7);

    /// <summary>
    /// Aqua color.
    /// </summary>
    public static readonly Color Aqua = FromBgra(0xFF00FFFF);

    /// <summary>
    /// Aquamarine color.
    /// </summary>
    public static readonly Color Aquamarine = FromBgra(0xFF7FFFD4);

    /// <summary>
    /// Azure color.
    /// </summary>
    public static readonly Color Azure = FromBgra(0xFFF0FFFF);

    /// <summary>
    /// Beige color.
    /// </summary>
    public static readonly Color Beige = FromBgra(0xFFF5F5DC);

    /// <summary>
    /// Bisque color.
    /// </summary>
    public static readonly Color Bisque = FromBgra(0xFFFFE4C4);

    /// <summary>
    /// Black color.
    /// </summary>
    public static readonly Color Black = FromBgra(0xFF000000);

    /// <summary>
    /// BlanchedAlmond color.
    /// </summary>
    public static readonly Color BlanchedAlmond = FromBgra(0xFFFFEBCD);

    /// <summary>
    /// Blue color.
    /// </summary>
    public static readonly Color Blue = FromBgra(0xFF0000FF);

    /// <summary>
    /// BlueViolet color.
    /// </summary>
    public static readonly Color BlueViolet = FromBgra(0xFF8A2BE2);

    /// <summary>
    /// Brown color.
    /// </summary>
    public static readonly Color Brown = FromBgra(0xFFA52A2A);

    /// <summary>
    /// BurlyWood color.
    /// </summary>
    public static readonly Color BurlyWood = FromBgra(0xFFDEB887);

    /// <summary>
    /// CadetBlue color.
    /// </summary>
    public static readonly Color CadetBlue = FromBgra(0xFF5F9EA0);

    /// <summary>
    /// Chartreuse color.
    /// </summary>
    public static readonly Color Chartreuse = FromBgra(0xFF7FFF00);

    /// <summary>
    /// Chocolate color.
    /// </summary>
    public static readonly Color Chocolate = FromBgra(0xFFD2691E);

    /// <summary>
    /// Coral color.
    /// </summary>
    public static readonly Color Coral = FromBgra(0xFFFF7F50);

    /// <summary>
    /// CornflowerBlue color.
    /// </summary>
    public static readonly Color CornflowerBlue = FromBgra(0xFF6495ED);

    /// <summary>
    /// Cornsilk color.
    /// </summary>
    public static readonly Color Cornsilk = FromBgra(0xFFFFF8DC);

    /// <summary>
    /// Crimson color.
    /// </summary>
    public static readonly Color Crimson = FromBgra(0xFFDC143C);

    /// <summary>
    /// Cyan color.
    /// </summary>
    public static readonly Color Cyan = FromBgra(0xFF00FFFF);

    /// <summary>
    /// DarkBlue color.
    /// </summary>
    public static readonly Color DarkBlue = FromBgra(0xFF00008B);

    /// <summary>
    /// DarkCyan color.
    /// </summary>
    public static readonly Color DarkCyan = FromBgra(0xFF008B8B);

    /// <summary>
    /// DarkGoldenrod color.
    /// </summary>
    public static readonly Color DarkGoldenrod = FromBgra(0xFFB8860B);

    /// <summary>
    /// DarkGray color.
    /// </summary>
    public static readonly Color DarkGray = FromBgra(0xFFA9A9A9);

    /// <summary>
    /// DarkGreen color.
    /// </summary>
    public static readonly Color DarkGreen = FromBgra(0xFF006400);

    /// <summary>
    /// DarkKhaki color.
    /// </summary>
    public static readonly Color DarkKhaki = FromBgra(0xFFBDB76B);

    /// <summary>
    /// DarkMagenta color.
    /// </summary>
    public static readonly Color DarkMagenta = FromBgra(0xFF8B008B);

    /// <summary>
    /// DarkOliveGreen color.
    /// </summary>
    public static readonly Color DarkOliveGreen = FromBgra(0xFF556B2F);

    /// <summary>
    /// DarkOrange color.
    /// </summary>
    public static readonly Color DarkOrange = FromBgra(0xFFFF8C00);

    /// <summary>
    /// DarkOrchid color.
    /// </summary>
    public static readonly Color DarkOrchid = FromBgra(0xFF9932CC);

    /// <summary>
    /// DarkRed color.
    /// </summary>
    public static readonly Color DarkRed = FromBgra(0xFF8B0000);

    /// <summary>
    /// DarkSalmon color.
    /// </summary>
    public static readonly Color DarkSalmon = FromBgra(0xFFE9967A);

    /// <summary>
    /// DarkSeaGreen color.
    /// </summary>
    public static readonly Color DarkSeaGreen = FromBgra(0xFF8FBC8B);

    /// <summary>
    /// DarkSlateBlue color.
    /// </summary>
    public static readonly Color DarkSlateBlue = FromBgra(0xFF483D8B);

    /// <summary>
    /// DarkSlateGray color.
    /// </summary>
    public static readonly Color DarkSlateGray = FromBgra(0xFF2F4F4F);

    /// <summary>
    /// DarkTurquoise color.
    /// </summary>
    public static readonly Color DarkTurquoise = FromBgra(0xFF00CED1);

    /// <summary>
    /// DarkViolet color.
    /// </summary>
    public static readonly Color DarkViolet = FromBgra(0xFF9400D3);

    /// <summary>
    /// DeepPink color.
    /// </summary>
    public static readonly Color DeepPink = FromBgra(0xFFFF1493);

    /// <summary>
    /// DeepSkyBlue color.
    /// </summary>
    public static readonly Color DeepSkyBlue = FromBgra(0xFF00BFFF);

    /// <summary>
    /// DimGray color.
    /// </summary>
    public static readonly Color DimGray = FromBgra(0xFF696969);

    /// <summary>
    /// VeryDimGray color.
    /// </summary>
    public static readonly Color VeryDimGray = FromBgra(0xFF404040);

    /// <summary>
    /// DodgerBlue color.
    /// </summary>
    public static readonly Color DodgerBlue = FromBgra(0xFF1E90FF);

    /// <summary>
    /// Firebrick color.
    /// </summary>
    public static readonly Color Firebrick = FromBgra(0xFFB22222);

    /// <summary>
    /// FloralWhite color.
    /// </summary>
    public static readonly Color FloralWhite = FromBgra(0xFFFFFAF0);

    /// <summary>
    /// ForestGreen color.
    /// </summary>
    public static readonly Color ForestGreen = FromBgra(0xFF228B22);

    /// <summary>
    /// Fuchsia color.
    /// </summary>
    public static readonly Color Fuchsia = FromBgra(0xFFFF00FF);

    /// <summary>
    /// Gainsboro color.
    /// </summary>
    public static readonly Color Gainsboro = FromBgra(0xFFDCDCDC);

    /// <summary>
    /// GhostWhite color.
    /// </summary>
    public static readonly Color GhostWhite = FromBgra(0xFFF8F8FF);

    /// <summary>
    /// Gold color.
    /// </summary>
    public static readonly Color Gold = FromBgra(0xFFFFD700);

    /// <summary>
    /// Goldenrod color.
    /// </summary>
    public static readonly Color Goldenrod = FromBgra(0xFFDAA520);

    /// <summary>
    /// Gray color.
    /// </summary>
    public static readonly Color Gray = FromBgra(0xFF808080);

    /// <summary>
    /// Green color.
    /// </summary>
    public static readonly Color Green = FromBgra(0xFF008000);

    /// <summary>
    /// GreenYellow color.
    /// </summary>
    public static readonly Color GreenYellow = FromBgra(0xFFADFF2F);

    /// <summary>
    /// Honeydew color.
    /// </summary>
    public static readonly Color Honeydew = FromBgra(0xFFF0FFF0);

    /// <summary>
    /// HotPink color.
    /// </summary>
    public static readonly Color HotPink = FromBgra(0xFFFF69B4);

    /// <summary>
    /// IndianRed color.
    /// </summary>
    public static readonly Color IndianRed = FromBgra(0xFFCD5C5C);

    /// <summary>
    /// Indigo color.
    /// </summary>
    public static readonly Color Indigo = FromBgra(0xFF4B0082);

    /// <summary>
    /// Ivory color.
    /// </summary>
    public static readonly Color Ivory = FromBgra(0xFFFFFFF0);

    /// <summary>
    /// Khaki color.
    /// </summary>
    public static readonly Color Khaki = FromBgra(0xFFF0E68C);

    /// <summary>
    /// Lavender color.
    /// </summary>
    public static readonly Color Lavender = FromBgra(0xFFE6E6FA);

    /// <summary>
    /// LavenderBlush color.
    /// </summary>
    public static readonly Color LavenderBlush = FromBgra(0xFFFFF0F5);

    /// <summary>
    /// LawnGreen color.
    /// </summary>
    public static readonly Color LawnGreen = FromBgra(0xFF7CFC00);

    /// <summary>
    /// LemonChiffon color.
    /// </summary>
    public static readonly Color LemonChiffon = FromBgra(0xFFFFFACD);

    /// <summary>
    /// LightBlue color.
    /// </summary>
    public static readonly Color LightBlue = FromBgra(0xFFADD8E6);

    /// <summary>
    /// LightCoral color.
    /// </summary>
    public static readonly Color LightCoral = FromBgra(0xFFF08080);

    /// <summary>
    /// LightCyan color.
    /// </summary>
    public static readonly Color LightCyan = FromBgra(0xFFE0FFFF);

    /// <summary>
    /// LightGoldenrodYellow color.
    /// </summary>
    public static readonly Color LightGoldenrodYellow = FromBgra(0xFFFAFAD2);

    /// <summary>
    /// LightGray color.
    /// </summary>
    public static readonly Color LightGray = FromBgra(0xFFD3D3D3);

    /// <summary>
    /// LightGreen color.
    /// </summary>
    public static readonly Color LightGreen = FromBgra(0xFF90EE90);

    /// <summary>
    /// LightPink color.
    /// </summary>
    public static readonly Color LightPink = FromBgra(0xFFFFB6C1);

    /// <summary>
    /// LightSalmon color.
    /// </summary>
    public static readonly Color LightSalmon = FromBgra(0xFFFFA07A);

    /// <summary>
    /// LightSeaGreen color.
    /// </summary>
    public static readonly Color LightSeaGreen = FromBgra(0xFF20B2AA);

    /// <summary>
    /// LightSkyBlue color.
    /// </summary>
    public static readonly Color LightSkyBlue = FromBgra(0xFF87CEFA);

    /// <summary>
    /// LightSlateGray color.
    /// </summary>
    public static readonly Color LightSlateGray = FromBgra(0xFF778899);

    /// <summary>
    /// LightSteelBlue color.
    /// </summary>
    public static readonly Color LightSteelBlue = FromBgra(0xFFB0C4DE);

    /// <summary>
    /// LightYellow color.
    /// </summary>
    public static readonly Color LightYellow = FromBgra(0xFFFFFFE0);

    /// <summary>
    /// Lime color.
    /// </summary>
    public static readonly Color Lime = FromBgra(0xFF00FF00);

    /// <summary>
    /// LimeGreen color.
    /// </summary>
    public static readonly Color LimeGreen = FromBgra(0xFF32CD32);

    /// <summary>
    /// Linen color.
    /// </summary>
    public static readonly Color Linen = FromBgra(0xFFFAF0E6);

    /// <summary>
    /// Magenta color.
    /// </summary>
    public static readonly Color Magenta = FromBgra(0xFFFF00FF);

    /// <summary>
    /// Maroon color.
    /// </summary>
    public static readonly Color Maroon = FromBgra(0xFF800000);

    /// <summary>
    /// MediumAquamarine color.
    /// </summary>
    public static readonly Color MediumAquamarine = FromBgra(0xFF66CDAA);

    /// <summary>
    /// MediumBlue color.
    /// </summary>
    public static readonly Color MediumBlue = FromBgra(0xFF0000CD);

    /// <summary>
    /// MediumOrchid color.
    /// </summary>
    public static readonly Color MediumOrchid = FromBgra(0xFFBA55D3);

    /// <summary>
    /// MediumPurple color.
    /// </summary>
    public static readonly Color MediumPurple = FromBgra(0xFF9370DB);

    /// <summary>
    /// MediumSeaGreen color.
    /// </summary>
    public static readonly Color MediumSeaGreen = FromBgra(0xFF3CB371);

    /// <summary>
    /// MediumSlateBlue color.
    /// </summary>
    public static readonly Color MediumSlateBlue = FromBgra(0xFF7B68EE);

    /// <summary>
    /// MediumSpringGreen color.
    /// </summary>
    public static readonly Color MediumSpringGreen = FromBgra(0xFF00FA9A);

    /// <summary>
    /// MediumTurquoise color.
    /// </summary>
    public static readonly Color MediumTurquoise = FromBgra(0xFF48D1CC);

    /// <summary>
    /// MediumVioletRed color.
    /// </summary>
    public static readonly Color MediumVioletRed = FromBgra(0xFFC71585);

    /// <summary>
    /// MidnightBlue color.
    /// </summary>
    public static readonly Color MidnightBlue = FromBgra(0xFF191970);

    /// <summary>
    /// MintCream color.
    /// </summary>
    public static readonly Color MintCream = FromBgra(0xFFF5FFFA);

    /// <summary>
    /// MistyRose color.
    /// </summary>
    public static readonly Color MistyRose = FromBgra(0xFFFFE4E1);

    /// <summary>
    /// Moccasin color.
    /// </summary>
    public static readonly Color Moccasin = FromBgra(0xFFFFE4B5);

    /// <summary>
    /// NavajoWhite color.
    /// </summary>
    public static readonly Color NavajoWhite = FromBgra(0xFFFFDEAD);

    /// <summary>
    /// Navy color.
    /// </summary>
    public static readonly Color Navy = FromBgra(0xFF000080);

    /// <summary>
    /// OldLace color.
    /// </summary>
    public static readonly Color OldLace = FromBgra(0xFFFDF5E6);

    /// <summary>
    /// Olive color.
    /// </summary>
    public static readonly Color Olive = FromBgra(0xFF808000);

    /// <summary>
    /// OliveDrab color.
    /// </summary>
    public static readonly Color OliveDrab = FromBgra(0xFF6B8E23);

    /// <summary>
    /// Orange color.
    /// </summary>
    public static readonly Color Orange = FromBgra(0xFFFFA500);

    /// <summary>
    /// OrangeRed color.
    /// </summary>
    public static readonly Color OrangeRed = FromBgra(0xFFFF4500);

    /// <summary>
    /// Orchid color.
    /// </summary>
    public static readonly Color Orchid = FromBgra(0xFFDA70D6);

    /// <summary>
    /// PaleGoldenrod color.
    /// </summary>
    public static readonly Color PaleGoldenrod = FromBgra(0xFFEEE8AA);

    /// <summary>
    /// PaleGreen color.
    /// </summary>
    public static readonly Color PaleGreen = FromBgra(0xFF98FB98);

    /// <summary>
    /// PaleTurquoise color.
    /// </summary>
    public static readonly Color PaleTurquoise = FromBgra(0xFFAFEEEE);

    /// <summary>
    /// PaleVioletRed color.
    /// </summary>
    public static readonly Color PaleVioletRed = FromBgra(0xFFDB7093);

    /// <summary>
    /// PapayaWhip color.
    /// </summary>
    public static readonly Color PapayaWhip = FromBgra(0xFFFFEFD5);

    /// <summary>
    /// PeachPuff color.
    /// </summary>
    public static readonly Color PeachPuff = FromBgra(0xFFFFDAB9);

    /// <summary>
    /// Peru color.
    /// </summary>
    public static readonly Color Peru = FromBgra(0xFFCD853F);

    /// <summary>
    /// Pink color.
    /// </summary>
    public static readonly Color Pink = FromBgra(0xFFFFC0CB);

    /// <summary>
    /// Plum color.
    /// </summary>
    public static readonly Color Plum = FromBgra(0xFFDDA0DD);

    /// <summary>
    /// PowderBlue color.
    /// </summary>
    public static readonly Color PowderBlue = FromBgra(0xFFB0E0E6);

    /// <summary>
    /// Purple color.
    /// </summary>
    public static readonly Color Purple = FromBgra(0xFF800080);

    /// <summary>
    /// Red color.
    /// </summary>
    public static readonly Color Red = FromBgra(0xFFFF0000);

    /// <summary>
    /// RosyBrown color.
    /// </summary>
    public static readonly Color RosyBrown = FromBgra(0xFFBC8F8F);

    /// <summary>
    /// RoyalBlue color.
    /// </summary>
    public static readonly Color RoyalBlue = FromBgra(0xFF4169E1);

    /// <summary>
    /// SaddleBrown color.
    /// </summary>
    public static readonly Color SaddleBrown = FromBgra(0xFF8B4513);

    /// <summary>
    /// Salmon color.
    /// </summary>
    public static readonly Color Salmon = FromBgra(0xFFFA8072);

    /// <summary>
    /// SandyBrown color.
    /// </summary>
    public static readonly Color SandyBrown = FromBgra(0xFFF4A460);

    /// <summary>
    /// SeaGreen color.
    /// </summary>
    public static readonly Color SeaGreen = FromBgra(0xFF2E8B57);

    /// <summary>
    /// SeaShell color.
    /// </summary>
    public static readonly Color SeaShell = FromBgra(0xFFFFF5EE);

    /// <summary>
    /// Sienna color.
    /// </summary>
    public static readonly Color Sienna = FromBgra(0xFFA0522D);

    /// <summary>
    /// Silver color.
    /// </summary>
    public static readonly Color Silver = FromBgra(0xFFC0C0C0);

    /// <summary>
    /// SkyBlue color.
    /// </summary>
    public static readonly Color SkyBlue = FromBgra(0xFF87CEEB);

    /// <summary>
    /// SlateBlue color.
    /// </summary>
    public static readonly Color SlateBlue = FromBgra(0xFF6A5ACD);

    /// <summary>
    /// SlateGray color.
    /// </summary>
    public static readonly Color SlateGray = FromBgra(0xFF708090);

    /// <summary>
    /// Snow color.
    /// </summary>
    public static readonly Color Snow = FromBgra(0xFFFFFAFA);

    /// <summary>
    /// SpringGreen color.
    /// </summary>
    public static readonly Color SpringGreen = FromBgra(0xFF00FF7F);

    /// <summary>
    /// SteelBlue color.
    /// </summary>
    public static readonly Color SteelBlue = FromBgra(0xFF4682B4);

    /// <summary>
    /// Tan color.
    /// </summary>
    public static readonly Color Tan = FromBgra(0xFFD2B48C);

    /// <summary>
    /// Teal color.
    /// </summary>
    public static readonly Color Teal = FromBgra(0xFF008080);

    /// <summary>
    /// Thistle color.
    /// </summary>
    public static readonly Color Thistle = FromBgra(0xFFD8BFD8);

    /// <summary>
    /// Tomato color.
    /// </summary>
    public static readonly Color Tomato = FromBgra(0xFFFF6347);

    /// <summary>
    /// Turquoise color.
    /// </summary>
    public static readonly Color Turquoise = FromBgra(0xFF40E0D0);

    /// <summary>
    /// Violet color.
    /// </summary>
    public static readonly Color Violet = FromBgra(0xFFEE82EE);

    /// <summary>
    /// Wheat color.
    /// </summary>
    public static readonly Color Wheat = FromBgra(0xFFF5DEB3);

    /// <summary>
    /// White color.
    /// </summary>
    public static readonly Color White = FromBgra(0xFFFFFFFF);

    /// <summary>
    /// WhiteSmoke color.
    /// </summary>
    public static readonly Color WhiteSmoke = FromBgra(0xFFF5F5F5);

    /// <summary>
    /// Yellow color.
    /// </summary>
    public static readonly Color Yellow = FromBgra(0xFFFFFF00);

    /// <summary>
    /// YellowGreen color.
    /// </summary>
    public static readonly Color YellowGreen = FromBgra(0xFF9ACD32);
}
