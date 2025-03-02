using System;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Input;
using Stride.Engine;


namespace ##Namespace##
{
    /// <summary>
    /// A script which spawns a timed instance from a source prefab.
    /// </summary>
    public class ##Scriptname## : AsyncScript
    {

        private float timeIntervalCountdown = 0f;

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
        public bool Following { get; set; } = true;

        /// <summary>
        /// How long before the prefab instance is deleted, selectable by the user
        /// </summary>
        [DataMember(30)]
        [Display("Timeout")]
        public float InstanceTimeout = 3f;

        /// <summary>
        /// What event triggers the script. Currently it listens for a key press.
        /// </summary>
        [DataMember(40)]
        [Display("Trigger")]
        public Keys Key = Keys.Space;

        /// <summary>
        /// Set the time interval (in seconds) at which to spawn new instances. Set it to 0 to deactivate.
        /// </summary>
        [DataMember(50)]
        [Display("Interval")]
        public float TimeInterval { get; set; } = 0f;

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
            var isTriggered = Input.IsKeyPressed(Key);

            if (TimeInterval > 0)
            {
                timeIntervalCountdown -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (timeIntervalCountdown <= 0f)
                {
                    timeIntervalCountdown = TimeInterval;
                    isTriggered = true;
                }
            }

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
