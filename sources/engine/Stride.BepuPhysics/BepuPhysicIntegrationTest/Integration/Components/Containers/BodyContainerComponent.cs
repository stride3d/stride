using System.ComponentModel;
using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class BodyContainerComponent : ContainerComponent
    {
        private bool _kinematic = false;

        public bool Kinematic
        {
            get => _kinematic;
            set
            {
                _kinematic = value;
                if (ContainerData?.Exist == true)
                    ContainerData.BuildOrUpdateContainer();
            }
        }

        public BodyReference? GetPhysicBody()
        {
            return ContainerData?.BepuSimulation.Simulation.Bodies[ContainerData.BHandle];           
        }

    }
}
