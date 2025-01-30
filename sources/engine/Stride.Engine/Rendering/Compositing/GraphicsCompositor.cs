// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// The <c>GraphicsCompositor</c> class organizes how scenes are rendered in the Stride engine, providing extensive customization of the rendering pipeline.
    /// </summary>
    /// <remarks>
    /// This class handles the initialization and destruction of the render system, manages the cameras used in the composition, and controls the render stages and features.
    /// It provides entry points for the game compositor, a single view compositor, and a compositor used by the scene editor.
    /// <para>
    /// Key features of the <c>GraphicsCompositor</c> include:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Using one or multiple cameras</description></item>
    ///   <item><description>Filtering entities</description></item>
    ///   <item><description>Rendering to one or more render textures with different viewports</description></item>
    ///   <item><description>Setting HDR or LDR rendering</description></item>
    ///   <item><description>Applying post effects to a render target</description></item>
    ///   <item><description>Clearing a render target or only the depth buffer</description></item>
    ///   <item><description>Editable in the Game Studio and at runtime from scripts</description></item>
    /// </list>
    /// <para>
    /// For more information, see the
    /// <see href="https://doc.stride3d.net/latest/en/manual/graphics/graphics-compositor/index.html">Graphics Compositor</see> documentation.
    /// </para>
    /// </remarks>
    [DataSerializerGlobal(typeof(ReferenceSerializer<GraphicsCompositor>), Profile = "Content")]
    [ReferenceSerializer, ContentSerializer(typeof(DataContentSerializerWithReuse<GraphicsCompositor>))]
    [DataContract]
    // Needed for indirect serialization of RenderSystem.RenderStages and RenderSystem.RenderFeatures
    // TODO: we would like an attribute to specify that serializing through the interface type is fine in this case (bypass type detection)
    [DataSerializerGlobal(null, typeof(FastTrackingCollection<RenderStage>))]
    [DataSerializerGlobal(null, typeof(FastTrackingCollection<RootRenderFeature>))]
    public class GraphicsCompositor : RendererBase
    {
        /// <summary>
        /// A property key to get the current <see cref="GraphicsCompositor"/> from the <see cref="ComponentBase.Tags"/>.
        /// </summary>
        public static readonly PropertyKey<GraphicsCompositor> Current = new PropertyKey<GraphicsCompositor>("GraphicsCompositor.Current", typeof(GraphicsCompositor));

        private readonly List<SceneInstance> initializedSceneInstances = new List<SceneInstance>();
        private static readonly ProfilingKey RenderSystemCollectKey = new ProfilingKey("RenderSystem.Collect");
        private static readonly ProfilingKey GameCollectKey = new ProfilingKey("Game.Collect");
        private static readonly ProfilingKey RenderSystemExtractKey = new ProfilingKey("RenderSystem.Extract");
        private static readonly ProfilingKey GameDrawKey = new ProfilingKey("Game.Draw");
        private static readonly ProfilingKey RenderSystemPrepareKey = new ProfilingKey("RenderSystem.Prepare");
        private static readonly ProfilingKey RenderSystemFlushKey = new ProfilingKey("RenderSystem.Flush");
        private static readonly ProfilingKey RenderSystemResetKey = new ProfilingKey("RenderSystem.Reset");
        private static readonly ProfilingKey DrawCoreKey = new ProfilingKey("GraphicsCompositor.DrawCore");

        /// <summary>
        /// Gets the render system used with this graphics compositor.
        /// </summary>
        [DataMemberIgnore]
        public RenderSystem RenderSystem { get; } = new RenderSystem();

        /// <summary>
        /// Gets the cameras used by this composition.
        /// </summary>
        /// <value>The cameras.</value>
        /// <userdoc>The list of cameras used in the graphic pipeline</userdoc>
        [DataMember(10)]
        [Category]
        [MemberCollection(NotNullItems = true)]
        public SceneCameraSlotCollection Cameras { get; } = new SceneCameraSlotCollection();

        /// <summary>
        /// The list of render stages.
        /// </summary>
        [DataMember(20)]
        [Category]
        [MemberCollection(NotNullItems = true)]
        public IList<RenderStage> RenderStages => RenderSystem.RenderStages;

        /// <summary>
        /// The list of render features.
        /// </summary>
        [DataMember(30)]
        [Category]
        [MemberCollection(NotNullItems = true)]
        public IList<RootRenderFeature> RenderFeatures => RenderSystem.RenderFeatures;

        /// <summary>
        /// The entry point for the game compositor.
        /// </summary>
        public ISceneRenderer Game { get; set; }

        /// <summary>
        /// The entry point for a compositor that can render a single view.
        /// </summary>
        public ISceneRenderer SingleView { get; set; }

        /// <summary>
        /// The entry point for a compositor used by the scene editor.
        /// </summary>
        public ISceneRenderer Editor { get; set; }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            RenderSystem.Initialize(Context);
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            // Dispose renderers
            Game?.Dispose();

            // Cleanup created visibility groups
            foreach (var sceneInstance in initializedSceneInstances)
            {
                for (var i = 0; i < sceneInstance.VisibilityGroups.Count; i++)
                {
                    var visibilityGroup = sceneInstance.VisibilityGroups[i];
                    if (visibilityGroup.RenderSystem == RenderSystem)
                    {
                        sceneInstance.VisibilityGroups.RemoveAt(i);
                        break;
                    }
                }
            }

            RenderSystem.Dispose();

            base.Destroy();
        }

        /// <inheritdoc/>
        protected override void DrawCore(RenderDrawContext context)
        {
            if (Game != null)
            {
                using var _ = Profiler.Begin(DrawCoreKey);

                // Get or create VisibilityGroup for this RenderSystem + SceneInstance
                var sceneInstance = SceneInstance.GetCurrent(context.RenderContext);
                VisibilityGroup visibilityGroup = null;
                if (sceneInstance != null)
                {
                    // Find if VisibilityGroup
                    foreach (var currentVisibilityGroup in sceneInstance.VisibilityGroups)
                    {
                        if (currentVisibilityGroup.RenderSystem == RenderSystem)
                        {
                            visibilityGroup = currentVisibilityGroup;
                            break;
                        }
                    }

                    // If first time, let's create and register it
                    if (visibilityGroup == null)
                    {
                        sceneInstance.VisibilityGroups.Add(visibilityGroup = new VisibilityGroup(RenderSystem));
                        initializedSceneInstances.Add(sceneInstance);
                    }

                    // Reset & cleanup
                    visibilityGroup.Reset();
                }

                using (context.RenderContext.PushTagAndRestore(SceneInstance.CurrentVisibilityGroup, visibilityGroup))
                using (context.RenderContext.PushTagAndRestore(SceneInstance.CurrentRenderSystem, RenderSystem))
                using (context.RenderContext.PushTagAndRestore(SceneCameraSlotCollection.Current, Cameras))
                using (context.RenderContext.PushTagAndRestore(Current, this))
                {
                    // Set render system
                    context.RenderContext.RenderSystem = RenderSystem;
                    context.RenderContext.VisibilityGroup = visibilityGroup;

                    // Set start states for viewports and output (it will be used during the Collect phase)
                    var renderOutputs = new RenderOutputDescription();
                    renderOutputs.CaptureState(context.CommandList);
                    context.RenderContext.RenderOutput = renderOutputs;

                    var viewports = new ViewportState();
                    viewports.CaptureState(context.CommandList);
                    context.RenderContext.ViewportState = viewports;

                    try
                    {
                        
                        using (Profiler.Begin(GameCollectKey))
                        {
                            // Collect in the game graphics compositor: Setup features/stages, enumerate views and populates VisibilityGroup
                            Game.Collect(context.RenderContext);
                        }

                        using (Profiler.Begin(RenderSystemCollectKey))
                        {
                            // Collect in render features
                            RenderSystem.Collect(context.RenderContext);

                            // Collect visibile objects from each view (that were not properly collected previously)
                            if (visibilityGroup != null)
                            {
                                foreach (var view in RenderSystem.Views)
                                    visibilityGroup.TryCollect(view);
                            }
                        }

                        using (Profiler.Begin(RenderSystemExtractKey))
                        {
                            // Extract
                            RenderSystem.Extract(context.RenderContext);
                        }
                        using (Profiler.Begin(RenderSystemPrepareKey))
                        {
                            // Prepare
                            RenderSystem.Prepare(context);
                        }

                        using (Profiler.Begin(GameDrawKey))
                        {
                            // Draw using the game graphics compositor
                            Game.Draw(context);
                        }

                        using (Profiler.Begin(RenderSystemFlushKey))
                        {
                            // Flush
                            RenderSystem.Flush(context);
                        }
                    }
                    finally
                    {
                        using (Profiler.Begin(RenderSystemResetKey))
                        {
                            // Reset render context data
                            RenderSystem.Reset();
                        }
                    }
                }
            }
        }
    }
}
