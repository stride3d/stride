using Stride.DotRecast.Definitions;
using Stride.Engine;
using Stride.Games;

namespace Stride.DotRecast;
public class DotRecastNavigationProcessor : EntityProcessor<DotRecastBoundingBoxComponent>
{

    private INavigationCollider.NavigationColliderProcessor _navigationColliderProcessor;

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
    }

    public override void Update(GameTime time)
    {
        if(_pendingRebuild)
        {
            _pendingRebuild = false;
            throw new NotImplementedException();
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
        _pendingRebuild = true;
        throw new NotImplementedException();
    }

    private void OnNavigationColliderAdded(INavigationCollider component)
    {
        _pendingRebuild = true;
        throw new NotImplementedException();
    }

}
