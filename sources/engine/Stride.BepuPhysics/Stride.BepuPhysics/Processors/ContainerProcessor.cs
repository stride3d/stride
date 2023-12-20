using BepuPhysics;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.BepuPhysics.Processors
{
    public class ContainerProcessor : EntityProcessor<ContainerComponent>
    {
        private BepuConfiguration _bepuConfiguration = new();
        private IGame? _game = null;

        public ContainerProcessor()
        {
            Order = 10000;
        }
        protected override void OnSystemAdd()
        {
            var configService = Services.GetService<IGameSettingsService>();
            _bepuConfiguration = configService.Settings.Configurations.Get<BepuConfiguration>();
            _game = Services.GetService<IGame>();

            if (_bepuConfiguration.BepuSimulations.Count == 0)
            {
                _bepuConfiguration.BepuSimulations.Add(new BepuSimulation());
            }

            Services.AddService(_bepuConfiguration);
        }

        //protected override ContainerData GenerateComponentData(Entity entity, ContainerComponent component)
        //{
        //    if (_game == null)
        //        throw new NullReferenceException(nameof(_game));

        //    return new(component, _bepuConfiguration, _game);
        //}

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            if (_game == null)
                throw new NullReferenceException(nameof(_game));

            component.ContainerData = new(component, _bepuConfiguration, _game);
            component.ContainerData.RebuildContainer();
            component.Services = Services;
            var parent = GetComponentsInParents<ContainerComponent>(entity).FirstOrDefault();
            if (parent != null)
            {
                parent.ContainerData?.RebuildContainer();
            }
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            component.ContainerData?.DestroyContainer();
            component.ContainerData = null;

            var parent = GetComponentsInParents<ContainerComponent>(entity).FirstOrDefault();
            if (parent != null)
            {
                parent.ContainerData?.RebuildContainer();
            }
        }

        public override void Update(GameTime time)
        {
            base.Update(time);

            var dt = (float)time.Elapsed.TotalMilliseconds;
            if (dt == 0f)
                return;

            foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
            {
                if (!bepuSim.Enabled)
                    continue;

                var totalTimeStepInMillisec = dt * bepuSim.TimeWarp; //Calculate the theoretical time step of the simulation
                bepuSim.RemainingUpdateTime += totalTimeStepInMillisec; //Add it to the counter

                int stepCount = 0;
                while (bepuSim.RemainingUpdateTime >= bepuSim.SimulationFixedStep & stepCount < bepuSim.MaxStepPerFrame)
                {
                    var simTimeStepInSec = bepuSim.SimulationFixedStep / 1000f;
                    bepuSim.CallSimulationUpdate(simTimeStepInSec);//cal the SimulationUpdate with the real step time of the sim in secs
                    bepuSim.Simulation.Timestep(simTimeStepInSec, bepuSim.ThreadDispatcher); //perform physic simulation using bepuSim.SimulationFixedStep
                    bepuSim.ContactEvents.Flush(); //Fire events handlers stuffs.
                    bepuSim.RemainingUpdateTime -= bepuSim.SimulationFixedStep; //in millisec
                    stepCount++;
                    bepuSim.CallAfterSimulationUpdate(simTimeStepInSec);//cal the AfterSimulationUpdate with the real step time of the sim in secs
                }



#warning I don't think this should be user-controllable ? We don't provide control over the other parts of the engine when they run through the dispatcher and having it on or of doesn't (or rather shouldn't) actually change the result, just how fast it resolves
                // I guess it could make sense when running on a low power device, but at that point might as well make the change to Dispatcher itself
                //Nicogo : a performance test on a smallScene would be nice to be sure
                if (bepuSim.ParallelUpdate)
                {
                    Dispatcher.For(0, bepuSim.Simulation.Bodies.ActiveSet.Count, (i) => UpdateBodiesPositionFunction(bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i], bepuSim));
                }
                else
                {
                    for (int i = 0; i < bepuSim.Simulation.Bodies.ActiveSet.Count; i++)
                    {
                        UpdateBodiesPositionFunction(bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i], bepuSim);
                    }
                }
            }
        }
        private static void UpdateBodiesPositionFunction(BodyHandle handle, BepuSimulation bepuSim)
        {
            var bodyContainer = bepuSim.BodiesContainers[handle];
            var body = bepuSim.Simulation.Bodies[handle];

            var parentEntityPosition = new Vector3();
            var parentEntityRotation = Quaternion.Identity;
            var parent = bodyContainer.Entity.Transform.Parent;
            if (parent != null)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out parentEntityRotation, out parentEntityPosition);
            }

            var entityTransform = bodyContainer.Entity.Transform;

            Vector3 localPosition = (body.Pose.Position.ToStrideVector() - bodyContainer.CenterOfMass - parentEntityPosition);
            entityTransform.Position = Vector3.Transform(localPosition, Quaternion.Invert(parentEntityRotation));
            entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion() * Quaternion.Invert(parentEntityRotation);

            if (bodyContainer.ChildsContainerComponent.Count > 0)
            {
                entityTransform.UpdateWorldMatrix(); //Warning this may cause threading-race issues (but i did large tests and never had issues)
                foreach (var item in bodyContainer.ChildsContainerComponent)
                {
                    //We need to call 
                    if (item.ContainerData != null && !item.ContainerData.IsStatic)
                        UpdateBodiesPositionFunction(item.ContainerData.BHandle, bepuSim);
                }
            }

        }

        private static IEnumerable<Entity> GetParents(Entity entity, bool includeMyself = false)
        {
            if (includeMyself)
                yield return entity;

            var parent = entity.GetParent();
            while (parent != null)
            {
                yield return parent;
                parent = parent.GetParent(); //Here
            }
        }
        private static IEnumerable<T> GetComponentsInParents<T>(Entity entity, bool includeMyself = false) where T : EntityComponent
        {
            foreach (var parent in GetParents(entity, includeMyself))
            {
                if (parent.Get<T>() is T component)
                    yield return component;
            }
        }

    }
}
