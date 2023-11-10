using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Configurations;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using System.Collections.Generic;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ColliderProcessor : EntityProcessor<ColliderComponent>
	{
		private List<BepuSimulation> _simulationComponents = new();
		// When any Colliders are in a scene a Simulation configuration is created.
		private BepuConfiguration _bepuconfiguration;

		public ColliderProcessor()
        {
            Order = 10020;
		}

		protected override void OnSystemAdd()
		{
			_bepuconfiguration = Services.GetService<BepuConfiguration>();
			// Create a default config if the user did not add it to gamestudio
			if( _bepuconfiguration == null ) 
			{
				_bepuconfiguration = new BepuConfiguration();
				_bepuconfiguration.BepuSimulations.Add(new BepuSimulation());
				Services.AddService(_bepuconfiguration);
			}
			_simulationComponents = _bepuconfiguration.BepuSimulations;
		}

		protected override void OnEntityComponentAdding(Entity entity, [NotNull] ColliderComponent component, [NotNull] ColliderComponent data)
        {
            component.Container?.ContainerData?.BuildShape();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ColliderComponent component, [NotNull] ColliderComponent data)
        {
            component.Container?.ContainerData?.BuildShape();
		}

		public override void Update(GameTime time)
		{
			var dt = (float)time.Elapsed.TotalSeconds;
			if (dt == 0f)
				return;

			foreach (var item in _simulationComponents)
			{
				if (!item.Enabled)
					continue;

				var SimTimeStep = dt * item.TimeWrap; //calculate the timeStep of the simulation

				item.CallSimulationUpdate(SimTimeStep); //calculate the SimulationUpdate with simTimeStep
				item.Simulation.Timestep(SimTimeStep, item.ThreadDispatcher); //perform physic sim using simTimeStep

				for (int i = 0; i < item.Simulation.Bodies.ActiveSet.Count; i++) //Update active body positions and rotation.
				{
					var handle = item.Simulation.Bodies.ActiveSet.IndexToHandle[i];
					var entity = item.Bodies[handle];
					var body = item.Simulation.Bodies[handle];

					var entityTransform = entity.Transform;
					entityTransform.Position = body.Pose.Position.ToStrideVector();
					entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
					entityTransform.UpdateWorldMatrix();
				}
			}
		}
	}

}
