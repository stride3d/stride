// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Editor.Build;

namespace Stride.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts a time to a number of frames at the animation frame rate of the game settings of the session (30 without a
/// session), and back.
/// </summary>
public sealed class TimeToFrames : ValueConverterBase<TimeToFrames>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan time)
            return null;

        // The first frame of a time just under a frame boundary, as a time that rounds down
        return (double)(long)(time.TotalSeconds * FrameRate + 0.1);
    }

    /// <inheritdoc/>
    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var frames = value is IConvertible convertible ? convertible.ToDouble(culture) : 0.0;
        return TimeSpan.FromSeconds(frames / FrameRate);
    }

    private static double FrameRate
    {
        get
        {
            var gameSettings = SessionViewModel.CurrentInstance?.ServiceProvider.TryGet<GameSettingsProviderService>()?.CurrentGameSettings;
            return Math.Max(gameSettings?.GetOrCreate<EditorSettings>().AnimationFrameRate ?? 30, 1.0);
        }
    }
}
