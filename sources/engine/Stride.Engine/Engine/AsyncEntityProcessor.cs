using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.MicroThreading;

namespace Stride.Engine
{
    public abstract class AsyncEntityProcessor : EntityProcessorBase
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
        public virtual Task Execute()
        {
            return Task.CompletedTask;
        }

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

    public abstract class AsyncEntityProcessor<TComponent> : AsyncEntityProcessor<TComponent, TComponent> where TComponent : EntityComponent
    {
        protected AsyncEntityProcessor([NotNull] params Type[] requiredAdditionalTypes)
            : base(requiredAdditionalTypes)
        {
        }

        /// <inheritdoc />
        protected override TComponent GenerateComponentData(Entity entity, TComponent component)
        {
            return component;
        }

        /// <inheritdoc />
        protected override bool IsAssociatedDataValid(Entity entity, TComponent component, TComponent associatedData)
        {
            return component == associatedData;
        }
    }

    public abstract class AsyncEntityProcessor<TComponent, TData> : AsyncEntityProcessor where TData : class where TComponent : EntityComponent
    {
        protected readonly ConcurrentDictionary<TComponent, TData> ComponentDatas = new ConcurrentDictionary<TComponent, TData>();
        private readonly HashSet<Entity> reentrancyCheck = new HashSet<Entity>();
        private readonly FastList<TypeInfo> checkRequiredTypes = new FastList<TypeInfo>();

        protected AsyncEntityProcessor([NotNull] params Type[] requiredAdditionalTypes)
            : base(typeof(TComponent), requiredAdditionalTypes)
        {
        }

        /// <inheritdoc/>
        protected internal override void OnSystemAdd()
        {
        }

        /// <inheritdoc/>
        protected internal override void OnSystemRemove()
        {
        }

        /// <inheritdoc/>
        protected internal override void RemoveAllEntities()
        {
            // Keep removing until empty
            while (ComponentDatas.Count > 0)
            {
                foreach (var component in ComponentDatas)
                {
                    ProcessEntityComponent(component.Key.Entity, component.Key, true);
                    break; // break right after since we remove from iterator
                }
            }
        }

        /// <inheritdoc/>
        protected internal override void ProcessEntityComponent(Entity entity, EntityComponent entityComponentArg, bool forceRemove)
        {
            // TODO: Entity component processing is currently synchronous, but can it be made fully asynchronous?
            var entityComponent = (TComponent)entityComponentArg;
            // If forceRemove is true, no need to check if entity matches.
            var entityMatch = !forceRemove && EntityMatch(entity);
            var entityAdded = ComponentDatas.TryGetValue(entityComponent, out var entityData);

            if (entityMatch && !entityAdded)
            {
                // Adding entity is not reentrant, so let's skip if already being called for current entity
                // (could happen if either GenerateComponentData, OnEntityPrepare or OnEntityAdd changes
                // any Entity components
                lock (reentrancyCheck)
                {
                    if (!reentrancyCheck.Add(entity))
                        return;
                }

                // Generate associated data
                var data = GenerateComponentData(entity, entityComponent);

                // Notify component being added
                OnEntityComponentAdding(entity, entityComponent, data);

                // Associate the component to its data
                ComponentDatas.TryAdd(entityComponent, data);

                lock (reentrancyCheck)
                {
                    reentrancyCheck.Remove(entity);
                }
            }
            else if (entityAdded && !entityMatch)
            {
                // Notify component being removed
                OnEntityComponentRemoved(entity, entityComponent, entityData);

                // Removes it from the component => data map
                ComponentDatas.TryRemove(entityComponent, out _);
            }
            else if (entityMatch) // && entityMatch
            {
                if (!IsAssociatedDataValid(entity, entityComponent, entityData))
                {
                    OnEntityComponentRemoved(entity, entityComponent, entityData);
                    entityData = GenerateComponentData(entity, entityComponent);
                    OnEntityComponentAdding(entity, entityComponent, entityData);
                    ComponentDatas.TryUpdate(entityComponent, entityData, ComponentDatas[entityComponent]);
                }
            }
        }

        /// <summary>Generates associated data to the given entity.</summary>
        /// Called right before <see cref="OnEntityComponentAdding"/>.
        /// <param name="entity">The entity.</param>
        /// <param name="component"></param>
        /// <returns>The associated data.</returns>
        [NotNull]
        protected abstract TData GenerateComponentData([NotNull] Entity entity, [NotNull] TComponent component);

        /// <summary>Checks if the current associated data is valid, or if readding the entity is required.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component"></param>
        /// <param name="associatedData">The associated data.</param>
        /// <returns>True if the change in associated data requires the entity to be readded, false otherwise.</returns>
        protected virtual bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] TComponent component, [NotNull] TData associatedData)
        {
            return GenerateComponentData(entity, component).Equals(associatedData);
        }

        /// <summary>Run when a matching entity is added to this entity processor.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component"></param>
        /// <param name="data">  The associated data.</param>
        protected virtual void OnEntityComponentAdding(Entity entity, [NotNull] TComponent component, [NotNull] TData data)
        {
        }

        /// <summary>Run when a matching entity is removed from this entity processor.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component"></param>
        /// <param name="data">  The associated data.</param>
        protected virtual void OnEntityComponentRemoved(Entity entity, [NotNull] TComponent component, [NotNull] TData data)
        {
        }

        private bool EntityMatch(Entity entity)
        {
            // When a processor has no requirement components, it always match with at least the component of entity
            if (!HasRequiredComponents)
            {
                return true;
            }

            checkRequiredTypes.Clear();
            for (var i = 0; i < RequiredTypes.Length; i++)
            {
                checkRequiredTypes.Add(RequiredTypes[i]);
            }

            var components = entity.Components;
            for (var i = 0; i < components.Count; i++)
            {
                var componentType = components[i].GetType().GetTypeInfo();
                for (var j = checkRequiredTypes.Count - 1; j >= 0; j--)
                {
                    if (checkRequiredTypes.Items[j].IsAssignableFrom(componentType))
                    {
                        checkRequiredTypes.RemoveAt(j);

                        if (checkRequiredTypes.Count == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            // If we are here, it means that required types were not found, so return false
            return false;
        }
    }
}
