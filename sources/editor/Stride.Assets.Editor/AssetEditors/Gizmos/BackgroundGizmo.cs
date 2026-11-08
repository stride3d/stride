// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Engine;
using Stride.Engine.Gizmos;

namespace Stride.Assets.Editor.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(BackgroundComponent), true)]
    public class BackgroundGizmo : BillboardingGizmo<BackgroundComponent>
    {
        public BackgroundGizmo(EntityComponent component)
            : base(component, "Background", GizmoResources.BackgroundGizmo)
        {
        }
    }
}
