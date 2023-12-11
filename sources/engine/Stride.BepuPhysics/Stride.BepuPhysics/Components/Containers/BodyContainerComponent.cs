using BepuPhysics;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class BodyContainerComponent : ContainerComponent
    {
        private bool _kinematic = false;
        private float _sleepThreshold = 0.01f;
        private byte _minimumTimestepCountUnderThreshold = 32;

        public bool Kinematic
        {
            get => _kinematic;
            set
            {
                _kinematic = value;
                ContainerData?.TryUpdateContainer();
            }
        }
        public float SleepThreshold
        {
            get => _sleepThreshold;
            set
            {
                _sleepThreshold = value;
                ContainerData?.TryUpdateContainer();
            }
        }
        public byte MinimumTimestepCountUnderThreshold
        {
            get => _minimumTimestepCountUnderThreshold;
            set
            {
                _minimumTimestepCountUnderThreshold = value;
                ContainerData?.TryUpdateContainer();
            }
        }

        #warning Users should not have to interact with this method directly, have a look at the car component to see how awkward it is to use
        // the struct doesn't seem safe to store either, what happens if the body is removed from the sim but users still interact with the struct, they're affecting the body that replaced it in that slot, right ?
        // We could copy the method that struct contains into this component and call them from here, hiding away the additional nonsense we have to deal with
        public BodyReference? GetPhysicBody()
        {
            return ContainerData?.BepuSimulation.Simulation.Bodies[ContainerData.BHandle];
        }
    }
}
