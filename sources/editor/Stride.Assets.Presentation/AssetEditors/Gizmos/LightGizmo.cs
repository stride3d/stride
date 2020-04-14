// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Lights;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// The base class for all light gizmos
    /// </summary>
    public class LightGizmo : BillboardingGizmo<LightComponent>
    {
        private static readonly LightComponent DefaultLightComponent = new LightComponent();

        protected LightComponent LightComponent => ContentEntity.Get<LightComponent>() ?? DefaultLightComponent;

        protected LightGizmo(EntityComponent component, string lightName, byte[] lightImageData)
            : base(component,lightName, lightImageData)
        {
        }

        protected Color3 GetLightColor(GraphicsDevice graphicsDevice)
        {
            var component = LightComponent;
            var colorLight = component.Type as IColorLight;

            return colorLight?.ComputeColor(graphicsDevice.ColorSpace, 1f) ?? new Color3(); // don't want to include light intensity in gizmo color (post effects not applied on gizmos)
        }
    }
}
