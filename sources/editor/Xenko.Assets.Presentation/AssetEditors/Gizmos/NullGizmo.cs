// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Reflection;
using Xenko.Assets.Presentation.SceneEditor;
using Xenko.Engine;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
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
