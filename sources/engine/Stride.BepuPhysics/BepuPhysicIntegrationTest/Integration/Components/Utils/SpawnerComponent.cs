using System;
using System.Linq;
using System.Windows.Media.Media3D;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using SharpDX.MediaFoundation;
using Silk.NET.OpenGL;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class SpawnerComponent : Spawner
    {
        public Entity SpawnPosition { get; set; } //set it from a (empty) entity at the wanted location

        public int Count { get; set; } = 100; //number of prefab to spawn
        public int SpawnByFrame { get; set; } = -60; //how much by frames (if SpawnByFrame < 0, it will be the number of update call for spawning 1 prefab)

        public Vector3 SpawnVelocity { get; set; } = new(0f, 20f, 0f); //the base velocity of the spawned prefab
        public Vector3 SpawnVelocityRange { get; set; } = new(2f, 0f, 2f); //XYZ * rand[-1,1]


        private int currentCount = 0;
        private int currentFrame = 0;


        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.P))
            {
                SpawnByFrame++;
            }
            if (Input.IsKeyPressed(Keys.M))
            {
                SpawnByFrame--;
            }
            DebugText.Print($"Prefab : {currentCount}/{Count}", new(Extensions.X_TEXT_POS, 25));
            DebugText.Print($"Spawn by frame : {SpawnByFrame} (p & m)", new(Extensions.X_TEXT_POS, 50));

            if (currentCount >= Count)
                return;

            var countToSpawn = SpawnByFrame;
            if (SpawnByFrame <= 0)
            {
                currentFrame++;
                if (currentFrame < -SpawnByFrame)
                    return;

                currentFrame = 0;
                countToSpawn = 1;
            }

            var minus1to1 = () => (Random.Shared.NextSingle() * 2f - 1f);

            for (int i = 0; i < countToSpawn && currentCount < Count; i++)
            {
                var vel = SpawnVelocity.ToNumericVector()  + (SpawnVelocityRange.ToNumericVector() * new System.Numerics.Vector3(minus1to1(), minus1to1(), minus1to1()));
                Spawn(SpawnPosition.Transform.Position, vel.ToStrideVector(), new());
                currentCount++;
            }
        }


    }
}
