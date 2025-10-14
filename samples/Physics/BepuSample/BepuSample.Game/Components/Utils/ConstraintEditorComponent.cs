// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Constraints;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;


namespace BepuSample.Game.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class ConstraintEditorComponent : SyncScript
    {
        public ConstraintComponentBase? Component { get; set; }


        public override void Start()
        {
        }

        public override void Update()
        {
            if (Component == null || !(Component is BallSocketConstraintComponent))
                return;

            if (Input.IsKeyPressed(Keys.I))
            {
                ((BallSocketConstraintComponent)Component).LocalOffsetB += new Vector3(0, 1, 0);
            }
            if (Input.IsKeyPressed(Keys.K))
            {
                ((BallSocketConstraintComponent)Component).LocalOffsetB -= new Vector3(0, 1, 0);
            }

            DebugText.Print($"LocalOffsetB : {((BallSocketConstraintComponent)Component).LocalOffsetB} (numpad i & k)", new(Game.Window.PreferredWindowedSize.X - 500, 300));
        }
    }
}
