using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions;
using Stride.Core.Annotations;
using Stride.Engine;
using static BepuPhysics.Collidables.CompoundBuilder;

namespace Stride.BepuPhysics.Navigation.Processors;
public class BepuStaticColliderProcessor : EntityProcessor<StaticContainerComponent>
{
	public delegate void CollectionChangedEventHandler(StaticContainerComponent component);

	public event CollectionChangedEventHandler ColliderAdded;
	public event CollectionChangedEventHandler ColliderRemoved;

	/// <summary>
	/// This is done based on the assumption that storing the data is cheaper than generating it from Bepu.
	/// More testing is needed to confirm this.
	/// </summary>
	public Dictionary<StaticContainerComponent, BodyShapeData> BodyShapes = new();

	private SceneSystem _sceneSystem;
	private EntityProcessor _entityProcessor;

	public BepuStaticColliderProcessor()
	{
		// this is done to ensure that this processor runs after the BepuPhysicsProcessors
		Order = 20001;
	}

	protected override void OnSystemAdd()
	{
		base.OnSystemAdd();

		_sceneSystem = Services.GetService<SceneSystem>();
		_entityProcessor = _sceneSystem.SceneInstance.GetProcessor<EntityProcessor>();
		
		foreach(var entity in _entityProcessor.EntityManager)
		{
			var container = entity.Get<StaticContainerComponent>();
			if (container != null)
			{
				BodyShapes.Add(container, container.GetShapeData());
			}
		}
	}

	protected override StaticContainerComponent GenerateComponentData(Entity entity, StaticContainerComponent component)
	{
		return base.GenerateComponentData(entity, component);
	}

	protected override bool IsAssociatedDataValid(Entity entity, StaticContainerComponent component, StaticContainerComponent associatedData)
	{
		// need to check for both StaticColliderComponent and StaticMeshContainerComponent
		if((StaticMeshContainerComponent)component is not null)
		{
			return true;
		}

		return true;
	}

	protected override void OnEntityComponentAdding(Entity entity, [NotNull] StaticContainerComponent component, [NotNull] StaticContainerComponent data)
	{
		BodyShapes.TryAdd(data, data.GetShapeData());
		ColliderAdded?.Invoke(data);
	}

	protected override void OnEntityComponentRemoved(Entity entity, [NotNull] StaticContainerComponent component, [NotNull] StaticContainerComponent data)
	{
		BodyShapes.Remove(data);
		ColliderRemoved?.Invoke(data);
	}

}
