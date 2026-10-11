// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Editor;
using Stride.Engine;
using Stride.SpriteStudio.Editor;
using Stride.SpriteStudio.Runtime;

[assembly: TypeImage(typeof(SpriteStudioComponent), "SpriteStudioComponent.png")]
[assembly: TypeImage(typeof(SpriteStudioNodeLinkComponent), "SpriteStudioNodeLinkComponent.png")]

namespace Stride.SpriteStudio.Editor;

/// <summary>
/// The editor side of SpriteStudio: the sheet preview, thumbnails, entity factory, drop policy and render feature are
/// found by their attributes or interfaces; the node link picker is registered here.
/// </summary>
public sealed class SpriteStudioEditorPlugin : StrideAssetsPlugin
{
    public override void InitializeSession(SessionViewModel session)
    {
        session.AssetViewProperties.RegisterNodePresenterUpdater(new SpriteStudioNodeLinkNodeUpdater());
    }
}
