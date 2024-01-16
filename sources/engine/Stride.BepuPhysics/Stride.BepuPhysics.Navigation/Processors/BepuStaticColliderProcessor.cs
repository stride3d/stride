using SharpFont;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Processors;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Collections.Generic;
using static BepuPhysics.Collidables.CompoundBuilder;

namespace Stride.BepuPhysics.Navigation.Processors;
public class BepuStaticColliderProcessor : EntityProcessor<StaticContainerComponent>
{
	private SceneSystem? _sceneSystem;
	private EntityProcessor? _entityProcessor;

	public new Dictionary<StaticContainerComponent, StaticContainerComponent>.Enumerator ComponentDatas => base.ComponentDatas.GetEnumerator();

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
		
		/*foreach(var entity in _entityProcessor.EntityManager)
		{
			var container = entity.Get<StaticContainerComponent>();
			if (container != null)
			{
				foreach (var shape in container.GetShapeData())
				{
					BodyShapes.TryAdd(container, shape);
					// transform the points to world space
					for (int i = 0; i < shape.Points.Count; i++)
					{
						shape.Points[i] = Vector3.Transform(shape.Points[i], container.Orientation);
						shape.Points[i] = (shape.Points[i] + container.Entity.Transform.WorldMatrix.TranslationVector) + container.CenterOfMass;
					}
				}
			}
		}*/
	}

	protected override void OnEntityComponentAdding(Entity entity, [NotNull] StaticContainerComponent component, [NotNull] StaticContainerComponent data)
	{

	}

	protected override void OnEntityComponentRemoved(Entity entity, [NotNull] StaticContainerComponent component, [NotNull] StaticContainerComponent data)
	{

	}
}
