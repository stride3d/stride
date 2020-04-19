// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.ValueConverters;
using Stride.Editor.Build;

namespace Stride.Assets.Presentation.ValueConverters
{
    /// <summary>
    /// This value converter will convert any numeric value to integer. <see cref="ConvertBack"/> is supported and
    /// will convert the value to the target if it is numeric, otherwise it returns the value as-is.
    /// </summary>
    public class TimeToFrames : ValueConverterBase<TimeToFrames>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var gameSettings = SessionViewModel.Instance?.ServiceProvider.Get<GameSettingsProviderService>().CurrentGameSettings;
            var frameRate = (double) (gameSettings?.GetOrCreate<EditorSettings>().AnimationFrameRate ?? 30);
            frameRate = Math.Max(frameRate, 1.0);
            var timeSpan = (TimeSpan)value;
            return System.Convert.ChangeType(timeSpan.TotalSeconds * frameRate + 0.1, typeof(long));
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var gameSettings = SessionViewModel.Instance?.ServiceProvider.Get<GameSettingsProviderService>().CurrentGameSettings;

            var frameRate = (double)(gameSettings?.GetOrCreate<EditorSettings>().AnimationFrameRate ?? 30);

            frameRate = Math.Max(frameRate, 1.0);

            var scalar = (double)(value ?? default(double));
            return TimeSpan.FromSeconds(scalar / frameRate);
        }
    }
}

