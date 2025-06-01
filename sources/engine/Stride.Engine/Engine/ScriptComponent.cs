// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Audio;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Profiling;
using Stride.Rendering;
using Stride.Rendering.Sprites;
using Stride.Streaming;

namespace Stride.Engine
{
    /// <summary>
    /// Script component.
    /// </summary>
    [DataContract("ScriptComponent", Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ScriptProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [Display(Expand = ExpandRule.Once)]
    [AllowMultipleComponents]
    [ComponentOrder(1000)]
    [ComponentCategory("Scripts")]
    public abstract class ScriptComponent : EntityComponent, ICollectorHolder
    {
        public const uint LiveScriptingMask = 128;

        /// <summary>
        /// The global profiling key for scripts. Activate/deactivate this key to activate/deactivate profiling for all your scripts.
        /// </summary>
        public static readonly ProfilingKey ScriptGlobalProfilingKey = new ProfilingKey("Script");

        private static readonly Dictionary<Type, ProfilingKey> ScriptToProfilingKey = new Dictionary<Type, ProfilingKey>();

        private ProfilingKey profilingKey;

        private IGraphicsDeviceService graphicsDeviceService;
        private Logger logger;

        protected ScriptComponent()
        {
        }

        internal void Initialize(IServiceRegistry registry)
        {
            Services = registry;

            graphicsDeviceService = Services.GetSafeServiceAs<IGraphicsDeviceService>();

            Game = Services.GetSafeServiceAs<IGame>();
            Content = (ContentManager)Services.GetSafeServiceAs<IContentManager>();
            Streaming = Services.GetSafeServiceAs<StreamingManager>();
        }

        /// <summary>
        /// Gets the profiling key to activate/deactivate profiling for the current script class.
        /// </summary>
        [DataMemberIgnore]
        public ProfilingKey ProfilingKey
        {
            get
            {
                if (profilingKey != null)
                    return profilingKey;

                var scriptType = GetType();
                if (!ScriptToProfilingKey.TryGetValue(scriptType, out profilingKey))
                {
                    profilingKey = new ProfilingKey(ScriptGlobalProfilingKey, scriptType.FullName);
                    ScriptToProfilingKey[scriptType] = profilingKey;
                }

                return profilingKey;
            }
        }

        [DataMemberIgnore]
        public IServiceRegistry Services { get; private set; }

        [DataMemberIgnore]
        public IGame Game { get; private set; }

        [DataMemberIgnore]
        public ContentManager Content { get; private set; }

        [DataMemberIgnore]
        public GraphicsDevice GraphicsDevice => graphicsDeviceService?.GraphicsDevice;

        [DataMemberIgnore]
        public GameProfilingSystem GameProfiler 
        { 
            get
            {
                gameProfilerSystem ??= Services.GetSafeServiceAs<GameProfilingSystem>();
                return gameProfilerSystem;
            }
            private set
            {
                gameProfilerSystem = value;
            }
        }
        private GameProfilingSystem gameProfilerSystem;

        [DataMemberIgnore]
        public InputManager Input 
        { 
            get
            {
                inputManager ??= Services.GetSafeServiceAs<InputManager>();
                return inputManager;
            }
            private set
            {
                inputManager = value;
            }
        }
        private InputManager inputManager;

        [DataMemberIgnore]
        public ScriptSystem Script 
        { 
            get
            {
                scriptSystem ??= Services.GetSafeServiceAs<ScriptSystem>();
                return scriptSystem;
            }
            private set
            {
                scriptSystem = value;
            }
        }
        private ScriptSystem scriptSystem;

        [DataMemberIgnore]
        public SceneSystem SceneSystem 
        { 
            get
            {
                sceneSystem ??= Services.GetSafeServiceAs<SceneSystem>();
                return sceneSystem;
            }
            private set
            {
                sceneSystem = value;
            }
        }
        private SceneSystem sceneSystem;

        [DataMemberIgnore]
        public EffectSystem EffectSystem 
        { 
            get
            {
                effectSystem ??= Services.GetSafeServiceAs<EffectSystem>();
                return effectSystem;
            }
            private set
            {
                effectSystem = value;
            }
        }
        private EffectSystem effectSystem;

        [DataMemberIgnore]
        public DebugTextSystem DebugText 
        { 
            get
            {
                debugTextSystem ??= Services.GetSafeServiceAs<DebugTextSystem>();
                return debugTextSystem;
            }
            private set
            {
                debugTextSystem = value;
            }
        }
        private DebugTextSystem debugTextSystem;

        [DataMemberIgnore]
        public AudioSystem Audio
        {
            get
            {
                audioSystem ??= Services.GetSafeServiceAs<AudioSystem>();
                return audioSystem;
            }
            private set
            {
                audioSystem = value;
            }
        }
        private AudioSystem audioSystem;

        [DataMemberIgnore]
        public SpriteAnimationSystem SpriteAnimation
        {
            get
            {
                spriteAnimationSystem ??= Services.GetSafeServiceAs<SpriteAnimationSystem>();
                return spriteAnimationSystem;
            }
            private set
            {
                spriteAnimationSystem = value;
            }
        }
        private SpriteAnimationSystem spriteAnimationSystem;

        /// <summary>
        /// Gets the streaming system.
        /// </summary>
        /// <value>The streaming system.</value>
        [DataMemberIgnore]
        public StreamingManager Streaming { get; private set; }

        [DataMemberIgnore]
        protected Logger Log
        {
            get
            {
                if (logger != null)
                {
                    return logger;
                }

                var className = GetType().FullName;
                logger = GlobalLogger.GetLogger(className);
                return logger;
            }
        }

        private int priority;

        /// <summary>
        /// The priority this script will be scheduled with (compared to other scripts).
        /// </summary>
        /// <userdoc>The execution priority for this script. It applies to async, sync and startup scripts. Lower values mean earlier execution.</userdoc>
        [DefaultValue(0)]
        [DataMember(10000)]
        public int Priority
        {
            get { return priority; }
            set { priority = value; PriorityUpdated(); }
        }

        /// <summary>
        /// Determines whether the script is currently undergoing live reloading.
        /// </summary>
        public bool IsLiveReloading { get; internal set; }

        /// <summary>
        /// The object collector associated with this script.
        /// </summary>
        [DataMemberIgnore]
        public ObjectCollector Collector
        {
            get
            {
                collector.EnsureValid();
                return collector;
            }
        }

        private ObjectCollector collector;

        /// <summary>
        /// Internal helper function called when <see cref="Priority"/> is changed.
        /// </summary>
        protected internal virtual void PriorityUpdated()
        {
        }

        /// <summary>
        /// Called when the script's update loop is canceled.
        /// </summary>
        public virtual void Cancel()
        {
            collector.Dispose();
        }
    }
}
