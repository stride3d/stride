// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Editor;
using Stride.Engine;
using Stride.Physics;

[assembly: TypeImage(typeof(PhysicsComponent), "PhysicsComponent.png")]
[assembly: TypeImage(typeof(PhysicsConstraintComponent), "PhysicsConstraintComponent.png")]

namespace Stride.Physics.Editor;

/// <summary>
/// The editor side of Bullet physics and navigation: gizmos, the heightmap preview and thumbnail, the navigation mesh
/// overlay and entity factory are found by their attributes; the property grid updaters are registered here.
/// </summary>
public sealed class PhysicsEditorPlugin : StrideAssetsPlugin
{
    public override void InitializeSession(SessionViewModel session)
    {
        session.AssetViewProperties.RegisterNodePresenterUpdater(new PhysicsNodeLinkNodeUpdater());
        session.AssetViewProperties.RegisterNodePresenterUpdater(new NavigationNodeUpdater(session));
    }
}
