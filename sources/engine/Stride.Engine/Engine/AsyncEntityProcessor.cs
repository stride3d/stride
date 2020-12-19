using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.MicroThreading;

namespace Stride.Engine
{
    public abstract class AsyncEntityProcessor : EntityProcessor
    {
        public static readonly ProfilingKey ScriptGlobalProfilingKey = new ProfilingKey("AsyncProcessor");

        private static readonly Dictionary<Type, ProfilingKey> ProcessorToProfilingKey = new Dictionary<Type, ProfilingKey>();

        private ProfilingKey profilingKey;

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

                var processorType = GetType();
                if (!ProcessorToProfilingKey.TryGetValue(processorType, out profilingKey))
                {
                    profilingKey = new ProfilingKey(ScriptGlobalProfilingKey, processorType.FullName);
                    ProcessorToProfilingKey[processorType] = profilingKey;
                }

                return profilingKey;
            }
        }

        private int priority;

        /// <summary>
        /// The priority this processor will be scheduled with (compared to other processors).
        /// </summary>
        /// <userdoc>The execution priority for this processor. Lower values mean earlier execution.</userdoc>
        [DefaultValue(0)]
        [DataMember(10000)]
        public int Priority
        {
            get { return priority; }
            set { priority = value; PriorityUpdated(); }
        }

        [DataMemberIgnore]
        internal MicroThread MicroThread;

        [DataMemberIgnore]
        internal CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Gets a token indicating if the script execution was canceled.
        /// </summary>
        public CancellationToken CancellationToken => MicroThread.CancellationToken;

        protected AsyncEntityProcessor([NotNull] Type mainComponentType, [NotNull] Type[] additionalTypes)
            : base(mainComponentType, additionalTypes)
        {
        }

        /// <summary>
        /// Called once, as a microthread
        /// </summary>
        /// <returns></returns>
        public abstract Task Execute();

        /// <summary>
        /// Internal helper function called when <see cref="Priority"/> is changed.
        /// </summary>
        protected internal virtual void PriorityUpdated()
        {
            // Update micro thread priority
            if (MicroThread != null)
                MicroThread.Priority = Priority;
        }
    }

    public abstract class AsyncEntityProcessor<TComponent, TData> : AsyncEntityProcessor where TData : class where TComponent : EntityComponent
    {
        protected AsyncEntityProcessor([NotNull] params Type[] requiredAdditionalTypes)
            : base(typeof(TComponent), requiredAdditionalTypes)
        {
        }
    }

    public abstract class AsyncEntityProcessor<TComponent> : AsyncEntityProcessor<TComponent, TComponent> where TComponent : EntityComponent
    {
    }
}
