// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Engine.FlexibleProcessing
{
    public class ProcessorManager
    {
        /// <summary> Every processor created during runtime </summary>
        /// <remarks>
        /// Processor removed still have their slot filled in, with a null <see cref="ProcessorData.Instance"/> to remove insertion cost.
        /// </remarks>
        private readonly List<ProcessorData> processors = new();

        /// <summary> Takes the type of the component and returns an array of the processors which handle this component </summary>
        /// <remarks> Value is an array of indices pointing into the <see cref="processors"/> array </remarks>
        private readonly Dictionary<Type, int[]> compTypeToProcessorIndices = new();

        /// <summary> Takes the type of the processor and returns its position in the <see cref="processors"/> array </summary>
        private readonly Dictionary<Type, int> typeToIndex = new();

        /// <summary> The list of <see cref="IUpdateProcessor"/> sorted by its <see cref="IUpdateProcessor.Order"/> </summary>
        private readonly SortedList<ProcessorSortingKey, UpdateData> processorsToUpdate = new();

        /// <summary> The list of <see cref="IDrawProcessor"/> sorted by its <see cref="IDrawProcessor.Order"/> </summary>
        private readonly SortedList<ProcessorSortingKey, DrawData> processorsToDraw = new();


        private readonly IServiceRegistry registry;

        public ProcessorManager(IServiceRegistry registryParam)
        {
            registry = registryParam;
        }

        public void IntroduceComponent(EntityComponent _component)
        {
            if (_component is not IMarkedComponent component)
                return;

            var componentType = component.GetType();
            if (compTypeToProcessorIndices.TryGetValue(componentType, out var processorsIndex) == false) // Create cache for this type
            {
                // One component may have multiple different IComponent<,>, we must handle each definition independently
                // To do so we have to iterate over all the interfaces this component may contain
                var interfaces = componentType.GetInterfaces();
                var processorsForThisType = new List<int>();
                foreach (var type in interfaces)
                {
                    if (type.IsGenericType == false)
                        continue;

                    if (typeof(IComponent<,>) != type.GetGenericTypeDefinition())
                        continue; // This interface is not IComponent<,>

                    var processorType = type.GenericTypeArguments[0];
                    if (typeToIndex.TryGetValue(processorType, out var procIndex) == false)
                    {
                        processors.Add(new(null, processorType, 0));
                        typeToIndex[processorType] = procIndex = processors.Count - 1;
                    }

                    processorsForThisType.Add(procIndex);
                }

                compTypeToProcessorIndices[componentType] = processorsIndex = processorsForThisType.ToArray();
            }

            foreach (var procIndex in processorsIndex)
            {
                ref var pDataRef = ref CollectionsMarshal.AsSpan(processors)[procIndex];
                pDataRef.ComponentCount++;

                var instance = pDataRef.Instance;
                if (instance == null) // if the processor hasn't been created yet or was previously removed
                {
                    pDataRef.Instance = instance = (IProcessorBase)Activator.CreateInstance(pDataRef.Type)!;
                    if (instance is IUpdateProcessor update)
                        processorsToUpdate.Add(new(update.Order, pDataRef.Type), new(update, new ProfilingKey(GameProfilingKeys.GameUpdate, GetType().Name)));
                    if (instance is IDrawProcessor draw)
                        processorsToDraw.Add(new(draw.Order, pDataRef.Type), new(draw, new ProfilingKey(GameProfilingKeys.GameDraw, GetType().Name)));

                    instance.SystemAdded(registry);
                    // Don't read or write to pDataRef from here on since SystemAdded may have mutated processors' backing array
                }

                instance.OnComponentAdded(component);
            }
        }

        public void RemoveComponent(EntityComponent _component)
        {
            if (_component is not IMarkedComponent component)
                return;

            foreach (var procIndex in compTypeToProcessorIndices[component.GetType()])
            {
                ref var pDataRef = ref CollectionsMarshal.AsSpan(processors)[procIndex];
                pDataRef.ComponentCount--;
                pDataRef.Instance!.OnComponentRemoved(component);

                // Refresh reference as OnComponentRemoved may have mutated processors' backing array
                pDataRef = ref CollectionsMarshal.AsSpan(processors)[procIndex];
                if (pDataRef.ComponentCount == 0)
                {
                    var system = pDataRef.Instance!;
                    pDataRef.Instance = null;
                    if (system is IUpdateProcessor update)
                    {
                        for (int i = 0; i < processorsToUpdate.Count; i++)
                        {
                            var val = processorsToUpdate.GetValueAtIndex(i);
                            if (ReferenceEquals(val.Instance, update))
                            {
                                processorsToUpdate.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    if (system is IDrawProcessor draw)
                    {
                        for (int i = 0; i < processorsToDraw.Count; i++)
                        {
                            var val = processorsToDraw.GetValueAtIndex(i);
                            if (ReferenceEquals(val.Instance, draw))
                            {
                                processorsToDraw.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    system.SystemRemoved();
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            foreach (var (_, processor) in processorsToUpdate)
            {
                using (Profiler.Begin(processor.ProfilingKey))
                {
                    processor.Instance.Update(gameTime);
                }
            }
        }

        public void Draw(RenderContext context)
        {
            foreach (var (_, processor) in processorsToDraw)
            {
                using (Profiler.Begin(processor.ProfilingKey))
                {
                    processor.Instance.Draw(context);
                }
            }
        }

        private record struct UpdateData(IUpdateProcessor Instance, ProfilingKey ProfilingKey);
        private record struct DrawData(IDrawProcessor Instance, ProfilingKey ProfilingKey);
        private record struct ProcessorData(IProcessorBase? Instance, Type Type, int ComponentCount);

        private readonly record struct ProcessorSortingKey(int Order, Type Type) : IComparable<ProcessorSortingKey>
        {
            public int CompareTo(ProcessorSortingKey other)
            {
                int comp = Order.CompareTo(other.Order);
                if (comp == 0)
                    return string.Compare(Type.FullName, other.Type.FullName, StringComparison.Ordinal);

                return comp;
            }
        }
    }
}
