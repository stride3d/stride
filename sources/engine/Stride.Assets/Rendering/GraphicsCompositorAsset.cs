// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using Stride.Core.Reflection;
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
    [AssetFormatVersion(StrideConfig.LogicalPackageName, CurrentVersion, "2.1.0.2")]
    [AssetUpgrader(StrideConfig.LogicalPackageName, "2.1.0.2", "3.1.0.1", typeof(RenderingSplitUpgrader))]
    [AssetUpgrader(StrideConfig.LogicalPackageName, "3.1.0.1", "3.1.0.2", typeof(ParticleFeatureOwnershipUpgrader))]
    [AssetUpgrader(StrideConfig.LogicalPackageName, "3.1.0.2", "3.1.0.3", typeof(UIFeatureOwnershipUpgrader))]
    public partial class GraphicsCompositorAsset : Asset
    {
        private const string CurrentVersion = "3.1.0.3";

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
                    if (node.Tag != null && node.Tag.EndsWith(",Stride.Engine", StringComparison.Ordinal)
                        // Several types are still in Stride.Engine
                        && node.Tag != "!Stride.Rendering.Compositing.ForwardRenderer,Stride.Engine"
                        && node.Tag != "!Stride.Rendering.Compositing.SceneCameraRenderer,Stride.Engine")
                    {
                        node.Tag = node.Tag.Replace(",Stride.Engine", ",Stride.Rendering");
                    }
                }
            }
        }

        private sealed class ParticleFeatureOwnershipUpgrader : PluginFeatureOwnershipUpgrader
        {
            protected override string PluginPackage => "Stride.Particles";

            protected override string FeatureTag => "!Stride.Particles.Rendering.ParticleEmitterRenderFeature,Stride.Particles";
        }

        private sealed class UIFeatureOwnershipUpgrader : PluginFeatureOwnershipUpgrader
        {
            protected override string PluginPackage => "Stride.UI";

            protected override string FeatureTag => "!Stride.Rendering.UI.UIRenderFeature,Stride.UI";
        }

        // In a compositor derived from an engine compositor, the plugin's render feature becomes an owned item, so reconciling
        // with the base keeps it; in a project without the plugin, the item is removed.
        private abstract class PluginFeatureOwnershipUpgrader : AssetUpgraderBase
        {
            protected abstract string PluginPackage { get; }

            protected abstract string FeatureTag { get; }

            // DefaultGraphicsCompositorLevel10 and DefaultGraphicsCompositorLevel9 of Stride.Engine,
            // DefaultGraphicsCompositorVoxels of Stride.Voxels
            private static readonly string[] EngineCompositorIds =
            [
                "823a81bf-bac0-4552-9267-aeed499c40df",
                "9af53371-51ba-49fc-b420-ee7874892e75",
                "472e6944-3ddc-47db-8d6f-2e0a987475ec",
            ];

            private static bool DependsOn(PackageContainer container, string packageName)
                => container.FlattenedDependencies.Any(x => string.Equals(x.Name, packageName, StringComparison.OrdinalIgnoreCase));

            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                if (asset.Archetype == null)
                    return;
                var archetype = (string)asset.Archetype;
                if (!EngineCompositorIds.Any(id => archetype.StartsWith(id, StringComparison.OrdinalIgnoreCase)))
                    return;
                var renderFeatures = asset.RenderFeatures as DynamicYamlMapping;
                if (renderFeatures == null)
                    return;
                // Only a project whose dependencies are resolved (they hold Stride.Engine) is known to lack the plugin;
                // otherwise the item is kept
                var withoutPlugin = context.Package?.Container is { } container
                    && container is not StandalonePackage { IsDependencyPackage: true }
                    && DependsOn(container, "Stride.Engine")
                    && !string.Equals(container.Package.Meta.Name, PluginPackage, StringComparison.OrdinalIgnoreCase)
                    && !DependsOn(container, PluginPackage);

                var items = renderFeatures.Node.Children;
                foreach (var item in items.ToList())
                {
                    if (item.Value.Tag != FeatureTag || item.Key is not YamlScalarNode { Value: { } key })
                        continue;
                    // Already owned
                    if (key.EndsWith(OverrideType.New.ToText(), StringComparison.Ordinal) || key.EndsWith((OverrideType.New | OverrideType.Sealed).ToText(), StringComparison.Ordinal))
                        continue;

                    if (withoutPlugin)
                    {
                        items.RemoveAt(items.IndexOf(item.Key));
                        continue;
                    }

                    // A sealed item is owned and stays sealed
                    var sealedText = OverrideType.Sealed.ToText();
                    var newKey = key.EndsWith(sealedText, StringComparison.Ordinal)
                        ? key[..^sealedText.Length] + (OverrideType.New | OverrideType.Sealed).ToText()
                        : key + OverrideType.New.ToText();
                    var index = items.IndexOf(item.Key);
                    items.RemoveAt(index);
                    items.Insert(index, new YamlScalarNode(newKey), item.Value);
                }
            }
        }
    }
}
