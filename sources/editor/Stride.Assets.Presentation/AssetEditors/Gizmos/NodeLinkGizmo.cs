// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(ModelNodeLinkComponent), true)]
    public class NodeLinkGizmo : BillboardingGizmo<ModelNodeLinkComponent>
    {
        public NodeLinkGizmo(EntityComponent component)
            : base(component, "NodeLink", GizmoResources.NodeLinkGizmo)
        {
        }
    }
}
