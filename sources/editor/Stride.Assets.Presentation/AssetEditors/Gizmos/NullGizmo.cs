// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Reflection;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public class NullGizmo<T> : EntityGizmo<T> where T : EntityComponent, new()
    {
        protected override Entity Create()
        {
            return null;
        }

        public NullGizmo(EntityComponent component) : base(component)
        {
        }
    }
}
