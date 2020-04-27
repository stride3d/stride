using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates how to add a component to an entiy. 
    /// We also learn a way to automically create and attach a component to our entity. 
    /// </summary>
    public class AddingAComponentDemo : SyncScript
    {
        private AmmoComponent _ammoComponent1;
        private AmmoComponent _ammoComponent2;
        private AmmoComponent _ammoComponent3;

        public override void Start()
        {
            // We can add a new component to an entity using the 'Add' method.
            _ammoComponent1 = new AmmoComponent();
            Entity.Add(_ammoComponent1);

            // We can even add the component a second time
            _ammoComponent2 = new AmmoComponent();
            Entity.Add(_ammoComponent2);

            // Lets remove all components of type AmmoComponent
            Entity.RemoveAll<AmmoComponent>();


            // When there is no component of this type attached, but we like there to be one, we can create it automatically
            // NOTE: when a component is created like this, the 'Start' method will be called after this script's Update method has executed
            _ammoComponent3 = Entity.GetOrCreate<AmmoComponent>();
        }

        public override void Update()
        {
            DebugText.Print("Total of ammo of the automatically created AmmoComponent3: " + _ammoComponent3.GetTotalAmmo().ToString(), new Int2(440, 200));
        }
    }
}
