// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
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
    public PhysicsEditorPlugin()
    {
        // Game Studio never steps physics: user code reading the flag (an "in the editor" check) sees it as before
        Simulation.DisableSimulation = true;
    }

    /// <inheritdoc />
    public override void InitializeSession(ISessionPluginContext context)
    {
        base.InitializeSession(context);

        context.NodePresenterUpdaters.Register(new PhysicsNodeLinkNodeUpdater());
        context.NodePresenterUpdaters.Register(new NavigationNodeUpdater(context.Session));
    }
}
