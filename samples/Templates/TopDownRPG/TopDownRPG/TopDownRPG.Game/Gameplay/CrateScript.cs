// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Audio;
using Stride.Engine;
using Stride.Engine.Events;
using TopDownRPG.Core;

namespace TopDownRPG.Gameplay
{
    public class CrateScript : SyncScript
    {
        public Prefab CoinGetEffect { get; set; }

        public Prefab CoinSpawnModel { get; set; }

        [Display("Crate Breaking")]
        public Sound SoundEffect { get; set; }
        private SoundInstance sfxInstance;

        public Trigger Trigger { get; set; }

        private EventReceiver<bool> triggeredEvent;

        private bool activated = false;

        private float animationTime = 0;

        public override void Update()
        {
            // Check if the coin has been collected
            bool triggered;
            if (!activated && (triggeredEvent?.TryReceive(out triggered) ?? false))
            {
                CollisionStarted();
            }

            UpdateAnimation();
        }

        public void UpdateAnimation()
        {
            if (!activated)
                return;

            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            animationTime += dt * 8;
            var coinHeight = Math.Max(0, Math.Sin(animationTime));
            Entity.Transform.Position.Y = 1 + (float)coinHeight;

            var uniformScale = (float) Math.Max(0, Math.Min(1, (2 * Math.PI - animationTime) / Math.PI));
            Entity.Transform.Scale = new Vector3(uniformScale);
        }

        public override void Start()
        {
            base.Start();

            triggeredEvent = (Trigger != null) ? new EventReceiver<bool>(Trigger.TriggerEvent) : null;

            sfxInstance = SoundEffect?.CreateInstance();
            sfxInstance?.Stop();
        }

        protected void CollisionStarted()
        {
            activated = true;

            // Add visual effect
            var effectMatrix = Matrix.Translation(Entity.Transform.WorldMatrix.TranslationVector);
            this.SpawnPrefabInstance(CoinGetEffect, null, 1.8f, effectMatrix);

            Func<Task> cleanupTask = async () =>
            {
                await Game.WaitTime(TimeSpan.FromMilliseconds(1500));

                Game.RemoveEntity(Entity);
            };

            Script.AddTask(cleanupTask);

            // Play a sound effect
            sfxInstance?.Play();

            // Spawn a collectible coin
            // CoinSpawnModel
            Random rand = new Random();
            var numCoins = 3 + rand.Next(4);
            for (int i = 0; i < numCoins; i++)
            {
                var offsetVector = new Vector3((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble(), (float)rand.NextDouble() - 0.5f);
                effectMatrix = Matrix.Scaling(0.7f + (float)rand.NextDouble() * 0.3f) * Matrix.Translation(Entity.Transform.WorldMatrix.TranslationVector + offsetVector * 2f);
                this.SpawnPrefabModel(CoinSpawnModel, null, effectMatrix, offsetVector * (3f + (float)rand.NextDouble() * 3f));
            }
        }
    }
}
