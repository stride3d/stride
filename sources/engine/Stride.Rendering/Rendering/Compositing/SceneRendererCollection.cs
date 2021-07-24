// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections;
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// A collection of <see cref="ISceneRenderer"/>.
    /// </summary>
    public partial class SceneRendererCollection : SceneRendererBase, IEnumerable<ISceneRenderer>
    {
        [Display(Expand = ExpandRule.Always)]
        public List<ISceneRenderer> Children { get; } = new List<ISceneRenderer>();

        protected override void CollectCore(RenderContext context)
        {
            base.CollectCore(context);

            foreach (var child in Children)
                child.Collect(context);
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            foreach (var child in Children)
                child.Draw(drawContext);
        }

        public void Add(ISceneRenderer child)
        {
            Children.Add(child);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ISceneRenderer> GetEnumerator()
        {
            return Children.GetEnumerator();
        }
    }
}
