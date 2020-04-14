// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// The gizmo dispatcher for lights
    /// </summary>
    [GizmoComponent(typeof(LightComponent), true)]
    public class DispatcherLightGizmo : DispatcherGizmo<LightComponent>
    {
        private class LightAmbientGizmo : LightGizmo
        {
            public LightAmbientGizmo(EntityComponent component)
                : base(component, "Ambient", GizmoResources.AmbientLightGizmo)
            {
            }
        }
        
        private class LightSkyboxGizmo : LightGizmo
        {
            public LightSkyboxGizmo(EntityComponent component)
                : base(component, "SkyboxLight", GizmoResources.SkyboxLightGizmo)
            {
            }
        }
        
        protected override Type GetGizmoType()
        {
            if (Component.Type is LightDirectional)
            {
                return typeof(LightDirectionalGizmo);
            }
            if (Component.Type is LightSpot)
            {
                return typeof(LightSpotGizmo);
            }
            if (Component.Type is LightAmbient)
            {
                return typeof(LightAmbientGizmo);
            }
            if (Component.Type is LightSkybox)
            {
                return typeof(LightSkyboxGizmo);
            }
            if (Component.Type is LightPoint)
            {
                return typeof(LightPointGizmo);
            }

            return null;
        }

        public DispatcherLightGizmo(EntityComponent component) : base(component)
        {
        }
    }
}
