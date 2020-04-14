// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// Used by editor as top level compositor.
    /// </summary>
    [NonInstantiable]
    public partial class EditorTopLevelCompositor : SceneExternalCameraRenderer, ISharedRenderer
    {
        /// <summary>
        /// When true, <see cref="PreviewGame"/> will be used as compositor.
        /// </summary>
        public bool EnablePreviewGame { get; set; }

        /// <summary>
        /// Compositor for previewing game, used when <see cref="EnablePreviewGame"/> is true.
        /// </summary>
        public ISceneRenderer PreviewGame { get; set; }

        public List<ISceneRenderer> PreGizmoCompositors { get; } = new List<ISceneRenderer>();

        public List<ISceneRenderer> PostGizmoCompositors { get; } = new List<ISceneRenderer>();

        protected override void CollectInner(RenderContext context)
        {
            if (EnablePreviewGame)
            {
                // Defer to PreviewGame
                PreviewGame?.Collect(context);
            }
            else
            {
                foreach (var gizmoCompositor in PreGizmoCompositors)
                    gizmoCompositor.Collect(context);

                base.CollectInner(context);

                foreach (var gizmoCompositor in PostGizmoCompositors)
                    gizmoCompositor.Collect(context);
            }
        }

        protected override void DrawInner(RenderDrawContext context)
        {
            if (EnablePreviewGame)
            {
                PreviewGame?.Draw(context);
            }
            else
            {
                foreach (var gizmoCompositor in PreGizmoCompositors)
                    gizmoCompositor.Draw(context);

                base.DrawInner(context);

                foreach (var gizmoCompositor in PostGizmoCompositors)
                    gizmoCompositor.Draw(context);
            }
        }
    }
}
