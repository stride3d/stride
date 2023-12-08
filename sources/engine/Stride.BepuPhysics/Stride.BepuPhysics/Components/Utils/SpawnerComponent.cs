using System;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class SpawnerComponent : Spawner
    {
        public Entity? SpawnPosition { get; set; } //set it from a (empty) entity at the wanted location

        public int Count { get; set; } = 100; //number of prefab to spawn
        public float SpawnRate { get; set; } = 1; //how much cube by sec.

        public Vector3 SpawnVelocity { get; set; } = new(0f, 20f, 0f); //the base velocity of the spawned prefab
        public Vector3 SpawnVelocityRange { get; set; } = new(2f, 0f, 2f); //XYZ * rand[-1,1]


        private int currentCount = 0;
        private float currentTime = 0;

        public override void SimulationUpdate(float timeStep)
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
                    var vel = SpawnVelocity.ToNumericVector() + SpawnVelocityRange.ToNumericVector() * new System.Numerics.Vector3(minus1to1(), minus1to1(), minus1to1());
                    Spawn(SpawnPosition.Transform.Position, vel.ToStrideVector(), new());
                    currentCount++;
                }
            }
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

            DebugText.Print($"Prefab count : {currentCount}/{Count}", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 25));
            DebugText.Print($"Spawn by Physic time : {SpawnRate} (p & m)", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 50));
        }
    }
}
