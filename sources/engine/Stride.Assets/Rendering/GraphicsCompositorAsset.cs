// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Rendering
{
    // TODO: this list is here just to easily identify object references. It can be removed once we have access to the path from IsObjectReference
    [DataContract]
    public class RenderStageCollection : List<RenderStage>
    {
    }

    // TODO: this list is here just to easily identify object references. It can be removed once we have access to the path from IsObjectReference
    [DataContract]
    public class SharedRendererCollection : TrackingCollection<ISharedRenderer>
    {
    }

    [DataContract("GraphicsCompositorAsset")]
    [AssetContentType(typeof(GraphicsCompositor))]
    [AssetDescription(FileExtension)]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.1.0.2")]
    [AssetUpgrader(StrideConfig.PackageName, "2.1.0.2", "3.1.0.1", typeof(RenderingSplitUpgrader))]
    public partial class GraphicsCompositorAsset : Asset
    {
        private const string CurrentVersion = "3.1.0.1";

        /// <summary>
        /// The default file extension used by the <see cref="GraphicsCompositorAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdgfxcomp";

        /// <summary>
        /// Gets the cameras used by this composition.
        /// </summary>
        /// <value>The cameras.</value>
        /// <userdoc>The list of cameras used in the graphic pipeline</userdoc>
        [Category("Camera Slots")]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public SceneCameraSlotCollection Cameras { get; } = new SceneCameraSlotCollection();

        /// <summary>
        /// The list of render stages.
        /// </summary>
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public RenderStageCollection RenderStages { get; } = new RenderStageCollection();

        /// <summary>
        /// The list of render features.
        /// </summary>
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public List<RootRenderFeature> RenderFeatures { get; } = new List<RootRenderFeature>();

        /// <summary>
        /// The list of graphics compositors.
        /// </summary>
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public SharedRendererCollection SharedRenderers { get; } = new SharedRendererCollection();

        /// <summary>
        /// The entry point for the game compositor.
        /// </summary>
        /// <userdoc>
        /// The renderer used by the game at runtime. It requires a properly set camera from the scene, found in the Cameras list.
        /// </userdoc>
        [Display("Game renderer", Expand = ExpandRule.Always)]
        public ISceneRenderer Game { get; set; }

        /// <summary>
        /// The entry point for a compositor that can render a single view.
        /// </summary>
        /// <userdoc>
        /// The utility renderer is used for rendering cubemaps, light maps, render-to-texture, etc. It should be a single-only view renderer with no post-processing. It doesn't require camera or render target, because they are supplied by the caller.
        /// </userdoc>
        [Display("Utility renderer")]
        public ISceneRenderer SingleView { get; set; }

        /// <summary>
        /// The entry point for a compositor used by the scene editor.
        /// </summary>
        /// <userdoc>
        /// The renderer used by the game studio while editing the scene. It can share the forward renderer with the game entry or not. It doesn't require a camera and uses the camera in the game studio instead.
        /// </userdoc>
        [Display("Editor renderer")]
        public ISceneRenderer Editor { get; set; }

        /// <summary>
        /// The positions of the blocks of the compositor in the editor canvas.
        /// </summary>
        [Display(Browsable = false)]
        public Dictionary<Guid, Vector2> BlockPositions { get; } = new Dictionary<Guid, Vector2>();

        public GraphicsCompositor Compile(bool copyRenderers)
        {
            var graphicsCompositor = new GraphicsCompositor();

            foreach (var cameraSlot in Cameras)
                graphicsCompositor.Cameras.Add(cameraSlot);
            foreach (var renderStage in RenderStages)
                graphicsCompositor.RenderStages.Add(renderStage);
            foreach (var renderFeature in RenderFeatures)
                graphicsCompositor.RenderFeatures.Add(renderFeature);

            if (copyRenderers)
            {
                graphicsCompositor.Game = Game;
                graphicsCompositor.SingleView = SingleView;
            }

            return graphicsCompositor;
        }

        // In 3.1, Stride.Engine was splitted into a sub-assembly Stride.Rendering
        private class RenderingSplitUpgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                YamlNode assetNode = asset.Node;
                foreach (var node in assetNode.AllNodes)
                {
                    if (node.Tag != null && node.Tag.EndsWith(",Stride.Engine")
                        // Several types are still in Stride.Engine
                        && node.Tag != "!Stride.Rendering.Compositing.ForwardRenderer,Stride.Engine"
                        && node.Tag != "!Stride.Rendering.Compositing.SceneCameraRenderer,Stride.Engine")
                    {
                        node.Tag = node.Tag.Replace(",Stride.Engine", ",Stride.Rendering");
                    }
                }
            }
        }
    }
}
