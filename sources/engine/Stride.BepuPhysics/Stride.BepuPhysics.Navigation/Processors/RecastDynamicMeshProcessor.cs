// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Navigation.Components;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Engine;
using Stride.Games;

namespace Stride.BepuPhysics.Navigation.Processors;
public class RecastDynamicMeshProcessor : EntityProcessor<BepuNavigationBoundingBoxComponent>
{
    public RecastMeshSystem RecastMeshSystem { get; set; } = null!;

    private IGame _game = null!;
    private SceneSystem _sceneSystem = null!;
    private ShapeCacheSystem _shapeCacheSystem = null!;

    private CollidableProcessor _collidableProcessor = null!;

    public RecastDynamicMeshProcessor()
    {
        Order = 1000;
    }

    protected override void OnSystemAdd()
    {
        ServicesHelper.LoadBepuServices(Services, out _, out _shapeCacheSystem, out _);
        _game = Services.GetSafeServiceAs<IGame>();
        _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();

        _collidableProcessor = _sceneSystem.SceneInstance.GetProcessor<CollidableProcessor>()!;
        _collidableProcessor.OnPostAdd += StartTrackingCollidable;
        _collidableProcessor.OnPreRemove += ClearTrackingForCollidable;
    }

    protected override void OnSystemRemove()
    {

    }

    protected override void OnEntityComponentAdding(Entity entity, BepuNavigationBoundingBoxComponent component, BepuNavigationBoundingBoxComponent data)
    {

    }

    protected override void OnEntityComponentRemoved(Entity entity, BepuNavigationBoundingBoxComponent component, BepuNavigationBoundingBoxComponent data)
    {

    }

    public override void Update(GameTime time)
    {

    }

    private void StartTrackingCollidable(CollidableComponent collidable)
    {

    }

    private void ClearTrackingForCollidable(CollidableComponent collidable)
    {

    }

    private void AddInitialColliders()
    {

    }
}
