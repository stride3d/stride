using Stride.Core;
using Stride.Core.Annotations;
using Stride.DotRecast.Definitions;
using Stride.Engine;
using Stride.Games;

namespace Stride.DotRecast;
public class DotRecastNavMeshProcessor : EntityProcessor<DotRecastNavMeshComponent>
{

    private INavigationCollider.NavigationColliderProcessor _navigationColliderProcessor;
    private SceneSystem _sceneSystem = null!;

    private bool _pendingRebuild = true;

    protected override void OnSystemAdd()
    {
        Services.AddService(this);

        // Check if the processor was added before the DotRecast collider processor
        var colliderProcessor = Services.GetService<INavigationCollider.NavigationColliderProcessor>();
        if(colliderProcessor != null)
        {
            InitializeNavigationColliderProcessor(colliderProcessor);
        }

        _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] DotRecastNavMeshComponent component, [NotNull] DotRecastNavMeshComponent data)
    {
        GetObjectsToBuild(component);
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] DotRecastNavMeshComponent component, [NotNull] DotRecastNavMeshComponent data)
    {

    }

    public override void Update(GameTime time)
    {
        if(_pendingRebuild)
        {


            _pendingRebuild = false;
        }
    }

    internal void InitializeNavigationColliderProcessor(INavigationCollider.NavigationColliderProcessor processor)
    {
        if (_navigationColliderProcessor != null)
        {
            _navigationColliderProcessor.ColliderAdded -= OnNavigationColliderAdded;
            _navigationColliderProcessor.ColliderRemoved -= OnNavigationColliderRemoved;
        }

        _navigationColliderProcessor = processor;

        if (_navigationColliderProcessor != null)
        {
            _navigationColliderProcessor.ColliderAdded += OnNavigationColliderAdded;
            _navigationColliderProcessor.ColliderRemoved += OnNavigationColliderRemoved;
        }
    }

    private void OnNavigationColliderRemoved(INavigationCollider component)
    {
        //_pendingRebuild = true;

    }

    private void OnNavigationColliderAdded(INavigationCollider component)
    {
        //_pendingRebuild = true;

    }

    private void GetObjectsToBuild(DotRecastNavMeshComponent component)
    {
        switch(component.CollectionMethod)
        {
            case DotRecastCollectionMethod.Scene:
                GetObjectsInScene(component);
                break;
            case DotRecastCollectionMethod.Children:
                GetObjectsInChildren(component);
                break;
            case DotRecastCollectionMethod.BoundingBox:
                throw new NotImplementedException("Bounding boxes are not yet supported for nav mesh generation.");
        }
    }

    private void GetObjectsInScene(DotRecastNavMeshComponent component)
    {
        //var entities = _sceneSystem.SceneInstance.Entities;
    }

    private void GetObjectsInChildren(DotRecastNavMeshComponent component)
    {
    }

}
