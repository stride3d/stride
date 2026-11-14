// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Gameplay
{
    public class SceneStreaming : SyncScript, IContactHandler
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
        /// The distance a collider has to enter the trigger before the scene starts loading asynchronously
        /// </summary>
        [DefaultValue(0.25f)]
        public float PreLoadDepth { get; set; } = 0.25f;

        /// <summary>
        /// The distance a collider has to enter the trigger before the scene starts loading synchronously
        /// </summary>
        [DefaultValue(0.5f)]
        public float LoadDepth { get; set; } = 0.5f;

        private bool shouldLoad = false;
        private bool shouldPreLoad = false;
        private bool shouldUnload = true;

        bool IContactHandler.NoContactResponse => false;

        void IContactHandler.OnTouching<TManifold>(Contacts<TManifold> contacts)
        {
            bool shouldLoad = false;
            bool shouldPreLoad = false;
            bool shouldUnload = true;

            foreach (var contact in contacts)
            {
                // Are there any contacts that are deep enough for synchronous loading?
                if (contact.Depth < -LoadDepth)
                {
                    shouldLoad = true;
                    shouldUnload = false;
                    break;
                }

                // Otherwise, are there any contacts that are deep enough for asynchronous loading?
                if (contact.Depth < -PreLoadDepth)
                {
                    shouldPreLoad = true;
                    shouldUnload = false;
                }
                else if (contact.Depth < 0.0f)
                {
                    // Are there any contacts at all?
                    shouldUnload = false;
                }
            }

            this.shouldLoad = shouldLoad;
            this.shouldPreLoad = shouldPreLoad;
            this.shouldUnload = shouldUnload;
        }

        public override void Update()
        {
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
