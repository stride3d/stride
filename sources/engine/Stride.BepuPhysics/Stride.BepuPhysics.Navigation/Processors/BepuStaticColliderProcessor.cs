using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Processors;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using static BepuPhysics.Collidables.CompoundBuilder;

namespace Stride.BepuPhysics.Navigation.Processors;
public class BepuStaticColliderProcessor : EntityProcessor<StaticContainerComponent>
{
	public delegate void CollectionChangedEventHandler(StaticContainerComponent component);

	public event CollectionChangedEventHandler? ColliderAdded;
	public event CollectionChangedEventHandler? ColliderRemoved;

	/// <summary>
	/// This is done based on the assumption that storing the data is cheaper than generating it from Bepu.
	/// More testing is needed to confirm this.
	/// </summary>
	public Dictionary<StaticContainerComponent, BodyShapeData> BodyShapes = new();

	private SceneSystem? _sceneSystem;
	private EntityProcessor? _entityProcessor;

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
				var shape = container.GetShapeData();
				// transform the points to world space
				for (int i = 0; i < shape.Points.Count; i++)
				{
					shape.Points[i] = Vector3.Transform(shape.Points[i], container.Entity.Transform.WorldMatrix).XYZ();
				}
				BodyShapes.Add(container, shape);
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

		return component is not null;
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
