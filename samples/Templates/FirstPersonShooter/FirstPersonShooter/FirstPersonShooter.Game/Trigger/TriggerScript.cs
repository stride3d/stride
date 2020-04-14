// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using FirstPersonShooter.Trigger;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace FirstPersonShooter
{
    /// <summary>
    /// A script which spawns a timed instance from a source prefab.
    /// </summary>
    public abstract class TriggerScript : AsyncScript
    {
        [DataMember(10)]
        [Display("TriggerGroup")]
        public TriggerGroup TriggerGroup { get; set; } = new TriggerGroup();

        protected void SpawnEvent(string eventName, Entity attachEntity, Matrix transformMatrix)
        {
            var TriggerEvent = TriggerGroup.Find(eventName);
            if (TriggerEvent == null)
                throw new TriggerGroupException($"Trigger event {eventName} was not found!");

            SpawnInstance(TriggerEvent.SourcePrefab, attachEntity, TriggerEvent.Duration, TriggerEvent.LocalMatrix * transformMatrix);
        }

        protected void SpawnInstance(Prefab source, Entity attachEntity, float timeout, Matrix localMatrix)
        {
            if (source == null)
                return;

            Func<Task> spawnTask = async () =>
            {
                // Clone
                var spawnedEntities = source.Instantiate();

                // Add
                foreach (var prefabEntity in spawnedEntities)
                {
                    prefabEntity.Transform.UpdateLocalMatrix();
                    var entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
                    entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation, out prefabEntity.Transform.Position);

                    if (attachEntity != null)
                    {
                        attachEntity.AddChild(prefabEntity);
                    }
                    else
                    {
                        SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                    }
                }

                // Countdown
                var secondsCountdown = timeout;
                while (secondsCountdown > 0f)
                {
                    await Script.NextFrame();
                    secondsCountdown -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                }

                // Remove
                foreach (var clonedEntity in spawnedEntities)
                {
                    if (attachEntity != null)
                    {
                        attachEntity.RemoveChild(clonedEntity);
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
