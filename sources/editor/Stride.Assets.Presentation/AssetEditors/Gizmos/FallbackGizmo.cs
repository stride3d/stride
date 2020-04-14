// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Engine;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    public class FallbackGizmo : BillboardingGizmo<TransformComponent>
    {
        public FallbackGizmo(EntityComponent component)
            : base(component, "Fallback", GizmoResources.FallbackGizmo)
        {
        }
    }
}
