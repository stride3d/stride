// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Input;
using Stride.Engine;

namespace ParticlesSample
{
    /// <summary>
    /// A script which spawns a timed instance from a source prefab.
    /// </summary>
    public class PrefabInstance : AsyncScript
    {
        /// <summary>
        /// Source to the prefab, selectable by the user
        /// </summary>
        [DataMember(10)]
        [Display("Source")]
        public Prefab SourcePrefab;

        /// <summary>
        /// Should the prefab follow the entity's transform component on change or not
        /// </summary>
        [DataMember(20)]
        [Display("Following")]
        public bool Following
        {
            get { return following; }
            set { following = value; }
        }
        private bool following = true;

        /// <summary>
        /// How long before the prefab instance is deleted, selectable by the user
        /// </summary>
        [DataMember(30)]
        [Display("Timeout")]
        public float InstanceTimeout = 3f;

        [DataMember(40)]
        [Display("Trigger Time")]
        public float TimeDelay { get; set; }

        [DataMember(50)]
        [Display("Animation")]
        public AnimationComponent Animation { get; set; }

        private bool canTrigger = true;
        private double lastTime = 0;

        public override async Task Execute()
        {
            while (Game.IsRunning)
            {
                await Script.NextFrame();

                if (IsTriggered())
                {
                    SpawnInstance();
                }
            }
        }

        protected bool IsTriggered()
        {
            var time = Animation?.PlayingAnimations[0].CurrentTime.TotalSeconds ?? 0;

            if (time < lastTime)
                canTrigger = true;

            var isTriggered = (canTrigger && TimeDelay <= time);

            lastTime = time;
            if (isTriggered)
                canTrigger = false;

            return isTriggered;
        }

        /// <summary>
        /// Will add a cloned entity from the prefab to the scene, wait for the specified time and delete it
        /// </summary>
        protected void SpawnInstance()
        {
            if (SourcePrefab == null)
                return;

            Func<Task> spawnTask = async () =>
            {
                // Clone
                var spawnedEntities = SourcePrefab.Instantiate();

                // Add
                foreach (var prefabEntity in spawnedEntities)
                {
                    if (Following)
                    {
                        Entity.AddChild(prefabEntity);
                    }
                    else
                    {
                        prefabEntity.Transform.UpdateLocalMatrix();
                        var worldMatrix = prefabEntity.Transform.LocalMatrix * Entity.Transform.WorldMatrix;
                        worldMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation, out prefabEntity.Transform.Position);

                        SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                    }
                }

                // Countdown
                var secondsCountdown = InstanceTimeout;
                while (secondsCountdown > 0f)
                {
                    await Script.NextFrame();
                    secondsCountdown -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                }

                // Remove
                foreach (var clonedEntity in spawnedEntities)
                {
                    if (Following)
                    {
                        Entity.RemoveChild(clonedEntity);
                    }
                    else
                    {
                        SceneSystem.SceneInstance.RootScene.Entities.Remove(clonedEntity);
                    }
                }

                // Cleanup
                spawnedEntities.Clear();
            };

            Script.AddTask(spawnTask);
        }
    }
}
