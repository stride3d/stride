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
    public class LootCoinScript : SyncScript
    {
        public Prefab CoinGetEffect { get; set; }

        public Trigger Trigger { get; set; }

        [Display("Collect Coin")]
        public Sound SoundEffect { get; set; }
        private SoundInstance sfxInstance;


        private EventReceiver<bool> triggeredEvent;

        private float spinSpeed = 1f;

        private bool activated = false;

        private float animationTime = (float)(Math.PI * 3 / 2);

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
            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            if (!activated)
                return;

            spinSpeed = Math.Min(10f, spinSpeed + dt * 15f);

            animationTime += dt * 5;
            if (animationTime > Math.PI * 3)
                Entity.Transform.Scale = Vector3.Zero;
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

            // Play a sound effect
            sfxInstance?.Play();

            // Add a visual effect
            var effectMatrix = Matrix.Translation(Entity.Transform.WorldMatrix.TranslationVector);
            this.SpawnPrefabInstance(CoinGetEffect, null, 3, effectMatrix);

            Func<Task> cleanupTask = async () =>
            {
                await Game.WaitTime(TimeSpan.FromMilliseconds(3000));

                Game.RemoveEntity(Entity.GetParent());
            };

            Script.AddTask(cleanupTask);
        }
    }
}
