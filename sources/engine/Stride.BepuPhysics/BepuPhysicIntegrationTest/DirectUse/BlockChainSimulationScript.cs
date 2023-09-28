//Code adapted from BEPU Samples !!

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysicIntegrationTest.Integration;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.DirectUse
{
    public class BlockChainSimulationScript : SyncScript
    {
        private ThreadDispatcher _threadDispatcher { get; set; }
        private BufferPool _bufferPool { get; set; }
        private Simulation _simulation { get; set; }
        private List<(BodyHandle handle, Entity entity)> _handleToEntity { get; set; } = new();

        public Prefab Cube { get; set; }
        public Entity Camera { get; set; }

        public override void Start()
        {
            var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
            _threadDispatcher = new ThreadDispatcher(targetThreadCount);
            _bufferPool = new BufferPool();
            _simulation = Simulation.Create(_bufferPool, new StrideNarrowPhaseCallbacks(new SpringSettings(30, 3)), new StridePoseIntegratorCallbacks(new Vector3(0, -10, 0), 0.1f, 0.5f), new SolveDescription(32, 4));//new SolveDescription(64, 8));

            var boxShape = new Box(1, 1, 1);
            var boxInertia = boxShape.ComputeInertia(1);
            var boxIndex = _simulation.Shapes.Add(boxShape);
            const int blocksPerChain = 20;

            //Build the blocks.
            for (int blockIndex = 0; blockIndex < blocksPerChain; ++blockIndex)
            {
                var desc = BodyDescription.CreateDynamic(new Vector3(0, 5 + blockIndex * boxShape.Height, 0), blockIndex == blocksPerChain - 1 ? new BodyInertia() : boxInertia, boxIndex, .01f);
                var hand = _simulation.Bodies.Add(desc);
                var enti = Cube.Instantiate().First();

                _handleToEntity.Add((hand, enti));
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
                _simulation.Solver.Add(_handleToEntity[i - 1].handle, _handleToEntity[i].handle, ballSocket);
            }

            base.Start();
        }

        public override void Update()
        {
            DebugText.Print("P - spawn cube", new(5, 10));
            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            dt = dt == 0 ? 0.01f : dt;

            _simulation.Timestep(dt, _threadDispatcher);

            if (Input.IsKeyDown(Keys.P))
            {
                var bulletShape = new Sphere(2);
                var forward = Stride.Core.Mathematics.Vector3.TransformNormal(-Stride.Core.Mathematics.Vector3.UnitZ, Stride.Core.Mathematics.Matrix.RotationQuaternion(Camera.Transform.Rotation)).ToNumericVector();
                var desc = BodyDescription.CreateConvexDynamic(Camera.Transform.Position.ToNumericVector(), forward * 100, bulletShape.Radius * bulletShape.Radius * bulletShape.Radius, _simulation.Shapes, bulletShape);
                var hand = _simulation.Bodies.Add(desc);
                var enti = Cube.Instantiate().First();

                _handleToEntity.Add((hand, enti));
                Entity.AddChild(enti);
            }

            for (int i = 0; i < _handleToEntity.Count; i++)
            {
                var strideTransform = _handleToEntity[i].entity.Transform;
                strideTransform.Position = _simulation.Bodies[_handleToEntity[i].handle].Pose.Position.ToStrideVector();
                strideTransform.Rotation = _simulation.Bodies[_handleToEntity[i].handle].Pose.Orientation.ToStrideQuaternion();
            }

        }
    }
}
