//Code adapted from BEPU Samples !!

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering.Compositing;

namespace PhysicsSample.Physics
{
    public class BlockChainSimulationScript : SyncScript
    {
        private ThreadDispatcher ThreadDispatcher { get; set; }
        private BufferPool BufferPool { get; set; }
        private Simulation Simulation { get; set; }

        public Prefab Cube { get; set; }
        public Entity Camera { get; set; }

        public List<(BodyHandle handle, Entity entity)> Data = new();

        public override void Start()
        {
            var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
            ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
            BufferPool = new BufferPool();
            Simulation = Simulation.Create(BufferPool, new StrideNarrowPhaseCallbacks(new SpringSettings(30, 3)), new StridePoseIntegratorCallbacks(new Vector3(0, -10, 0), 0.1f, 0.5f), new SolveDescription(32, 4));//new SolveDescription(64, 8));

            var boxShape = new Box(1, 1, 1);
            var boxInertia = boxShape.ComputeInertia(1);
            var boxIndex = Simulation.Shapes.Add(boxShape);
            const int blocksPerChain = 20;

            //Build the blocks.
            for (int blockIndex = 0; blockIndex < blocksPerChain; ++blockIndex)
            {
                var desc = BodyDescription.CreateDynamic(new Vector3(0, 5 + blockIndex * (boxShape.Height), 0), blockIndex == blocksPerChain - 1 ? new BodyInertia() : boxInertia, boxIndex, .01f);
                var hand = Simulation.Bodies.Add(desc);
                var enti = Cube.Instantiate().First();

                Data.Add((hand, enti));
                Entity.AddChild(enti);
            }

            //Build the chains.
            for (int i = 1; i < blocksPerChain; ++i)
            {
                var ballSocket = new BallSocket
                {
                    LocalOffsetA = new Vector3(0, 1f, 0),
                    LocalOffsetB = new Vector3(0, -1f, 0),
                    SpringSettings = new SpringSettings(30, 5)
                };
                Simulation.Solver.Add(Data[i - 1].handle, Data[i].handle, ballSocket);
            }

            base.Start();
        }

        public override void Update()
        {
            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            dt = (dt == 0 ? 0.01f : dt);

            Simulation.Timestep(dt, ThreadDispatcher);

            if (Input.IsKeyDown(Keys.P))
            {
                var bulletShape = new Sphere(2);
                var forward = Stride.Core.Mathematics.Vector3.TransformNormal(-Stride.Core.Mathematics.Vector3.UnitZ, Stride.Core.Mathematics.Matrix.RotationQuaternion(Camera.Transform.Rotation)).ToNumericVector();
                var desc = BodyDescription.CreateConvexDynamic(Camera.Transform.Position.ToNumericVector(), forward * 100, bulletShape.Radius * bulletShape.Radius * bulletShape.Radius, Simulation.Shapes, bulletShape);
                var hand = Simulation.Bodies.Add(desc);
                var enti = Cube.Instantiate().First();

                Data.Add((hand, enti));
                Entity.AddChild(enti);
            }

            for (int i = 0; i < Data.Count; i++)
            {
                var strideTransform = Data[i].entity.Transform;
                strideTransform.Position = Simulation.Bodies[Data[i].handle].Pose.Position.ToStrideVector();
                strideTransform.Rotation = Simulation.Bodies[Data[i].handle].Pose.Orientation.ToStrideQuaternion();
            }


        }
    }
}
