// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class SpawnerComponent : Spawner, ISimulationUpdate
    {
        public Entity? SpawnPosition { get; set; } //set it from a (empty) entity at the wanted location

        public int Count { get; set; } = 100; //number of prefab to spawn
        public float SpawnRate { get; set; } = 1; //how much cube by sec.

        public Vector3 SpawnVelocity { get; set; } = new(0f, 20f, 0f); //the base velocity of the spawned prefab
        public Vector3 SpawnVelocityRange { get; set; } = new(2f, 0f, 2f); //XYZ * rand[-1,1]


        private int currentCount = 0;
        private float currentTime = 0;

        public void SimulationUpdate(BepuSimulation simulation, float timeStep)
        {
            if (SpawnPosition == null) return;

            if (SpawnRate < 0)
                SpawnRate = 0;

            if (currentCount < Count)
            {
                currentTime += timeStep;
                var toSpawn = (int)Math.Floor(currentTime * SpawnRate);
                if (toSpawn < 1)
                {

                    return;
                }

                currentTime -= toSpawn / SpawnRate;

                var minus1to1 = () => Random.Shared.NextSingle() * 2f - 1f;

                for (int i = 0; i < toSpawn && currentCount < Count; i++)
                {
                    var vel = SpawnVelocity + new Vector3(minus1to1() * SpawnVelocityRange.X, minus1to1() * SpawnVelocityRange.Y, minus1to1() * SpawnVelocityRange.Z);
                    Spawn(SpawnPosition.Transform.Position, vel, new());
                    currentCount++;
                }
            }
        }

        public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {

        }

        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.P))
            {
                SpawnRate *= 1.1f;
            }
            if (Input.IsKeyPressed(Keys.M))
            {
                SpawnRate /= 1.1f;
            }

            DebugText.Print($"Prefab count : {currentCount}/{Count}", new(Game.Window.PreferredWindowedSize.X - 500, 25));
            DebugText.Print($"Spawn by Physic time : {SpawnRate} (p & m)", new(Game.Window.PreferredWindowedSize.X - 500, 50));
        }
    }
}
