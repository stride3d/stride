// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Editor.Avalonia.Converters;
using Image = Stride.Graphics.Image;

namespace Stride.Assets.Editor.Avalonia.Converters;

/// <summary>
/// The converters of the sprite sheet editor view: the image of the selected sprite, the positions of its region, borders and
/// center in the view, and the cursors of the pixel tools.
/// </summary>
public static class SpriteEditorConverters
{
    /// <summary>
    /// The bitmap of a sprite image, from its source file (first value) and the image cache of the editor (second value).
    /// </summary>
    public static readonly IMultiValueConverter SourceToBitmap = new SourceToBitmapConverter();

    /// <summary>
    /// A number plus the number of the parameter, as the WPF <c>SumNum</c> converter.
    /// </summary>
    public static readonly IValueConverter AddDouble = new FuncValueConverter<double, double, double>((value, parameter) => value + parameter);

    /// <summary>
    /// The four distances of a <see cref="Vector4"/> (left, top, right, bottom) as a thickness, each plus the number of the parameter.
    /// </summary>
    public static readonly IValueConverter Vector4ToThickness = new FuncValueConverter<Vector4, double, Thickness>((value, parameter) => new Thickness(value.X + parameter, value.Y + parameter, value.Z + parameter, value.W + parameter));

    /// <summary>
    /// A point (<see cref="Vector2"/>) as the left and top of a thickness, each plus the number of the parameter.
    /// </summary>
    public static readonly IValueConverter Vector2ToThickness = new FuncValueConverter<Vector2, double, Thickness>((value, parameter) => new Thickness(value.X + parameter, value.Y + parameter, 0, 0));

    /// <summary>
    /// Four numbers (left, top, right, bottom) as a thickness, as the WPF <c>ThicknessMultiConverter</c>.
    /// </summary>
    public static readonly IMultiValueConverter ToThickness = new FuncMultiValueConverter<double, Thickness>(values =>
    {
        var list = values.ToList();
        return list.Count == 4 ? new Thickness(list[0], list[1], list[2], list[3]) : default;
    });

    /// <summary>
    /// The cursor of a tool of the editor: the WPF cursors of the color key picker and the magic wand, the default one otherwise.
    /// </summary>
    public static readonly IValueConverter ToolModeToCursor = new FuncValueConverter<SpriteSheetEditorToolMode, Cursor?>(mode => mode switch
    {
        SpriteSheetEditorToolMode.ColorPick => Cursors.ColorPicker,
        SpriteSheetEditorToolMode.MagicWand => Cursors.MagicWand,
        _ => null,
    });

    /// <summary>
    /// A name in camel case as words ("Sprite2D" as "Sprite 2D"), as the WPF <c>CamelCaseTextConverter</c>.
    /// </summary>
    public static readonly IValueConverter CamelCaseToWords = new FuncValueConverter<object?, string?>(value =>
        value?.ToString() is { } text ? Regex.Replace(text, "(?<=[a-z])(?=[A-Z0-9])|(?<=[0-9])(?=[A-Z][a-z])", " ") : null);

    private static class Cursors
    {
        // The WPF cursors (Stride.Assets.Presentation.Wpf/Resources/*.cur) as images, with the same hot spots
        public static readonly Cursor ColorPicker = Load("ColorPicker", new PixelPoint(3, 28));

        public static readonly Cursor MagicWand = Load("MagicWand", new PixelPoint(3, 3));

        private static Cursor Load(string name, PixelPoint hotSpot)
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri($"avares://Stride.Assets.Editor.Avalonia/Assets/Cursors/{name}.png"));
                return new Cursor(new Bitmap(stream), hotSpot);
            }
            catch (Exception)
            {
                // No cursor image (a headless test without the platform): the cross gives the same feedback
                return new Cursor(StandardCursorType.Cross);
            }
        }
    }

    private sealed class SourceToBitmapConverter : IMultiValueConverter
    {
        // A bitmap per image of the cache: the cache keeps the image until its file changes
        private static readonly ConditionalWeakTable<Image, Bitmap> Bitmaps = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2 || values[0] is not UFile source || values[1] is not SpriteEditorImageCache cache)
                return null;

            var image = cache.RetrieveImage(source);
            if (image is null)
                return null;

            if (!Bitmaps.TryGetValue(image, out var bitmap))
            {
                bitmap = ImageToBitmap.Convert(image);
                if (bitmap is null)
                    return null;
                Bitmaps.Add(image, bitmap);
            }
            return bitmap;
        }
    }
}
