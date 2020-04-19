// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Rendering.Compositing;

namespace Stride.Rendering
{
    /// <summary>
    /// Defines a view used during rendering. This is usually a frustum and some camera parameters.
    /// </summary>
    public class RenderView
    {
        /// <summary>
        /// The part of the view specific to a given <see cref="RootRenderFeature"/>.
        /// </summary>
        public readonly List<RenderViewFeature> Features = new List<RenderViewFeature>();

        /// <summary>
        /// List of data sepcific to each <see cref="RenderStage"/> for this <see cref="RenderView"/>.
        /// </summary>
        public readonly List<RenderViewStage> RenderStages = new List<RenderViewStage>();

        /// <summary>
        /// List of visible render objects.
        /// </summary>
        public readonly ConcurrentCollector<RenderObject> RenderObjects = new ConcurrentCollector<RenderObject>();

        /// <summary>
        /// Index in <see cref="RenderSystem.Views"/>.
        /// </summary>
        public int Index = -1;

        internal int LastFrameCollected;

        internal float MinimumDistance;

        internal float MaximumDistance;

        /// <summary>
        /// The view matrix for this view.
        /// </summary>
        public Matrix View = Matrix.Identity;

        /// <summary>
        /// The projection matrix for this view.
        /// </summary>
        public Matrix Projection = Matrix.Identity;

        /// <summary>
        /// The view projection matrix for this view.
        /// </summary>
        public Matrix ViewProjection;

        /// <summary>
        /// Far clip plane.
        /// </summary>
        public float NearClipPlane;

        /// <summary>
        /// Near clip plane.
        /// </summary>
        public float FarClipPlane;

        /// <summary>
        /// The frustum extracted from the view projection matrix.
        /// </summary>
        public BoundingFrustum Frustum;

        /// <summary>
        /// The size of the view being rendered.
        /// </summary>
        public Vector2 ViewSize;

        // TODO GRAPHICS REFACTOR likely to be replaced soon
        /// <summary>
        /// The culling mask.
        /// </summary>
        public RenderGroupMask CullingMask { get; set; } = RenderGroupMask.All;

        /// <summary>
        /// The culling mode.
        /// </summary>
        public CameraCullingMode CullingMode { get; set; } = CameraCullingMode.Frustum;

        public RenderViewFlags Flags { get; set; }

        /// <summary>
        /// The view used for lighting (useful to share lighting results for two very close views such as VR)
        /// </summary>
        /// <remarks>This is a temporary workaround until shadow maps have a real scope: global or view-dependent (single view or multiple views).</remarks>
        public RenderView LightingView { get; set; }

        // TODO: This should be configured by the creator of the view. E.g. near clipping can be enabled for spot light shadows.
        /// <summary>
        /// Ignore depth planes in visibility test
        /// </summary>
        public bool VisiblityIgnoreDepthPlanes = false;

        public override string ToString()
        {
            return $"RenderView ({Features.Sum(x => x.ViewObjectNodes.Count)} objects, {Features.Sum(x => x.RenderNodes.Count)} render nodes, {RenderStages.Count} stages)";
        }
    }
}
