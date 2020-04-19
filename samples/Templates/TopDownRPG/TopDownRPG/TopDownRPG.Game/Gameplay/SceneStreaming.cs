// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Physics;

namespace Gameplay
{
    public class SceneStreaming : SyncScript
    {
        private Task<Scene> loadingTask;
        private CancellationTokenSource loadCancellation;

        /// <summary>
        /// The loaded scene
        /// </summary>
        [DataMemberIgnore]
        public Scene Instance { get; private set; }

        /// <summary>
        /// The url of the scene to load
        /// </summary>
        public UrlReference<Scene> Url { get; set; }

        /// <summary>
        /// The trigger volume. This should be a static collider, set to be a trigger
        /// </summary>
        public PhysicsComponent Trigger { get; set; }

        /// <summary>
        /// The distance a collider has to enter the trigger before the scene starts loading asynchronously
        /// </summary>
        [DefaultValue(0.25f)]
        public float PreLoadDepth { get; set; } = 0.25f;

        /// <summary>
        /// The distance a collider has to enter the trigger before the scene starts loading synchronously
        /// </summary>
        [DefaultValue(0.5f)]
        public float LoadDepth { get; set; } = 0.5f;

        public override void Update()
        {
            if (Trigger == null)
                return;

            bool shouldLoad = false;
            bool shouldPreLoad = false;
            bool shouldUnload = true;

            foreach (var collision in Trigger.Collisions)
            {
                // Check all colliders that can collide with the trigger volume
                if ((collision.ColliderA.CanCollideWith & (CollisionFilterGroupFlags)collision.ColliderB.CollisionGroup) == 0 ||
                    (collision.ColliderB.CanCollideWith & (CollisionFilterGroupFlags)collision.ColliderA.CollisionGroup) == 0)
                    continue;

                foreach (var contact in collision.Contacts)
                {
                    // Are there any contacts that are deep enough for synchronous loading?
                    if (contact.Distance < -LoadDepth)
                    {
                        shouldLoad = true;
                        shouldUnload = false;
                        break;
                    }

                    // Otherwise, are there any contacts that are deep enough for asynchronous loading?
                    if (contact.Distance < -PreLoadDepth)
                    {
                        shouldPreLoad = true;
                        shouldUnload = false;
                    }
                    else if (contact.Distance < 0.0f)
                    {
                        // Are there any contacts at all?
                        shouldUnload = false;
                    }
                }

                if (shouldLoad)
                    break;
            }

            if (!shouldUnload)
            {
                // Loading is already in progress, or even finished
                if (loadingTask == null)
                {
                    if (shouldLoad)
                    {
                        // If we should load syncrhonously, just create a completed task and load 
                        Instance = Content.Load(Url);
                        loadingTask = Task.FromResult(Instance);
                    }
                    else if (shouldPreLoad)
                    {
                        loadCancellation = new CancellationTokenSource();

                        var localLoadingTask = loadingTask = Content.LoadAsync(Url);
                        Script.AddTask(async () =>
                        {
                            await loadingTask;

                            // Immediately unload if unload or sync load was triggered in the meantime
                            if (loadCancellation.IsCancellationRequested || loadingTask != localLoadingTask)
                            {
                                Content.Unload(localLoadingTask.Result);
                                loadCancellation = null;

                                // Unloading was triggered
                                if (loadingTask == localLoadingTask)
                                    loadingTask = null;
                            }
                            else
                            {
                                Instance = loadingTask.Result;
                            }
                        });
                    }
                }

                // Once loaded, add it to the scene
                if (Instance != null)
                {
                    Instance.Parent = Entity.Scene;
                }
            }
            else
            {
                // Cancel loading if currently in progress, and reset state
                loadCancellation?.Cancel();
                loadCancellation = null;
                loadingTask = null;

                // Unload if already finished
                if (Instance != null)
                {
                    Content.Unload(Instance);

                    // If we were the last user, detach. Ideally scripts should cooperate differently
                    if (!Content.IsLoaded(Url))
                    {
                        Instance.Parent = null;
                    }

                    Instance = null;
                }
            }
        }
    }
}
