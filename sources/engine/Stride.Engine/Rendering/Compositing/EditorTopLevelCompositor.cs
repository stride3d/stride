// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        /// The layer of the overlay gizmo drawn over every other one (the transform gizmo).
        /// </summary>
        public const int TopmostOverlayGizmoLayer = 100;

        private readonly List<(int Layer, ISceneRenderer Renderer)> overlayGizmoCompositors = new List<(int, ISceneRenderer)>();
        private readonly ClearRenderer overlayDepthClear = new ClearRenderer { ClearFlags = ClearRendererFlags.DepthOnly };

        /// <summary>
        /// When true, <see cref="PreviewGame"/> will be used as compositor.
        /// </summary>
        public bool EnablePreviewGame { get; set; }

        /// <summary>
        /// Compositor for previewing game, used when <see cref="EnablePreviewGame"/> is true.
        /// </summary>
        public ISceneRenderer PreviewGame { get; set; }

        public List<ISceneRenderer> PreGizmoCompositors { get; } = new List<ISceneRenderer>();

        /// <summary>
        /// Renderers drawn after the scene, depth-tested against it (grid, wireframes, pickers).
        /// </summary>
        public List<ISceneRenderer> PostGizmoCompositors { get; } = new List<ISceneRenderer>();

        /// <summary>
        /// Adds a renderer drawn after <see cref="PostGizmoCompositors"/> over a cleared depth buffer, so it shows through the scene.
        /// </summary>
        /// <param name="renderer">Group stages that share one depth clear in a <see cref="SceneRendererCollection"/>.</param>
        /// <param name="layer">Overlays draw by increasing layer, <see cref="TopmostOverlayGizmoLayer"/> last.</param>
        public void AddOverlayGizmoCompositor([NotNull] ISceneRenderer renderer, int layer = 0)
        {
            var index = overlayGizmoCompositors.FindLastIndex(x => x.Layer <= layer) + 1;
            overlayGizmoCompositors.Insert(index, (layer, renderer));
        }

        public bool RemoveOverlayGizmoCompositor([NotNull] ISceneRenderer renderer)
        {
            return overlayGizmoCompositors.RemoveAll(x => x.Renderer == renderer) > 0;
        }

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

                foreach (var overlay in overlayGizmoCompositors)
                {
                    overlayDepthClear.Collect(context);
                    overlay.Renderer.Collect(context);
                }
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

                foreach (var overlay in overlayGizmoCompositors)
                {
                    overlayDepthClear.Draw(context);
                    overlay.Renderer.Draw(context);
                }
            }
        }
    }
}
