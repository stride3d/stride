// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Mathematics;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;

namespace Stride.Engine
{
    /// <summary>
    /// Extensions for the <see cref="LightComponent"/> class.
    /// </summary>
    public static class LightComponentExtensions
    {
        /// <summary>
        /// Gets the color from a <see cref="LightComponent"/> assuming that the <see cref="LightComponent.Type"/> is an instance of <see cref="IColorLight"/> 
        /// </summary>
        /// <param name="light">The light component.</param>
        /// <returns>The color of the light component</returns>
        /// <exception cref="InvalidOperationException">If the LightComponent doesn't contain a color light type IColorLight</exception>
        public static Color3 GetColor(this LightComponent light)
        {
            var colorLight = light.Type as IColorLight;
            var lightColorRgb = colorLight?.Color as ColorRgbProvider;
            if (lightColorRgb != null)
            {
                return lightColorRgb.Value;
            }
            throw new InvalidOperationException("The LightComponent doesn't contain a color light type IColorLight");
        }

        /// <summary>
        /// Sets the color from a <see cref="LightComponent"/> assuming that the <see cref="LightComponent.Type"/> is an instance of <see cref="IColorLight"/> 
        /// </summary>
        /// <param name="light">The light component.</param>
        /// <param name="color">The light color.</param>
        /// <exception cref="InvalidOperationException">If the LightComponent doesn't contain a color light type IColorLight</exception>
        public static void SetColor(this LightComponent light, Color3 color)
        {
            var colorLight = light.Type as IColorLight;
            if (colorLight == null)
                throw new InvalidOperationException("The LightComponent doesn't contain a color light type IColorLight");

            var lightColorRgb = colorLight.Color as ColorRgbProvider ?? new ColorRgbProvider();
            lightColorRgb.Value = color;
            colorLight.Color = lightColorRgb;
        }
    }
}
