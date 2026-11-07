// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Editor;
using Stride.Video;
using Stride.Video.Assets;
using Stride.Video.Editor;

[assembly: TypeImage(typeof(VideoComponent), "VideoComponent.png")]
[assembly: StaticThumbnail(typeof(VideoAsset), "VideoThumbnail.png")]

namespace Stride.Video.Editor;

/// <summary>
/// Editor extensions of the Video package: registers the property grid updater; the scene drop policy, thumbnail,
/// type image and editor-game compiler are found by attribute in this assembly.
/// </summary>
public sealed class VideoEditorPlugin : StrideAssetsPlugin
{
    /// <inheritdoc />
    public override void InitializeSession(ISessionPluginContext context)
    {
        base.InitializeSession(context);

        context.NodePresenterUpdaters.Register(new VideoAssetNodeUpdater());
    }
}
