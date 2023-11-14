using BepuPhysicIntegrationTest.Integration.Components.ConstraintsV2;
using BepuPhysicIntegrationTest.Integration.Configurations;
using BepuPhysics.Constraints;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class ConstraintEditorComponent : SyncScript
    {
        public ConstraintComponent Component { get; set; }


        public override void Start()
        {
        }

        public override void Update()
        {
            var a = Component.ConstraintData.BepuSimulation.Simulation.Solver.GetConstraintReference(Component.ConstraintData.CHandle);
            if (Input.IsKeyPressed(Keys.I))
            {
                ((BallSocketConstraintComponent)Component).LocalOffsetB += new Stride.Core.Mathematics.Vector3(0, 1, 0);
            }
            if (Input.IsKeyPressed(Keys.K))
            {
                ((BallSocketConstraintComponent)Component).LocalOffsetB -= new Stride.Core.Mathematics.Vector3(0, 1, 0);
            }

            DebugText.Print($"LocalOffsetB : {((BallSocketConstraintComponent)Component).LocalOffsetB} (numpad i & k)", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 300));
        }
    }
}
