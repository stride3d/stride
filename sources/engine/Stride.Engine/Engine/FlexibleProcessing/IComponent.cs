// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Engine.FlexibleProcessing
{
    /// <summary>
    /// Defines a Processor/component or system/component relationship, <see cref="EntityComponent"/> implementing this interface
    /// will be sent to the <typeparamref name="TProcessor"/> defined. Enabling component addition and removal tracking as well as batching operations over components.
    /// </summary>
    /// <typeparam name="TProcessor">The system which collects those components and iterates over them</typeparam>
    /// <typeparam name="TThis">The type name implementing this interface</typeparam>
    public interface IComponent<TProcessor, TThis> : IMarkedComponent where TProcessor : IComponent<TProcessor, TThis>.IProcessor, new() where TThis : IComponent<TProcessor, TThis>
    {
        public interface IProcessor : IProcessorBase
        {
            /// <summary> Occurs right after a component is added to the scene </summary>
            void OnComponentAdded(TThis item);
            /// <summary> Occurs right after a component is removed from the scene </summary>
            void OnComponentRemoved(TThis item);
            void IProcessorBase.OnComponentAdded(IMarkedComponent comp) => OnComponentAdded((TThis)comp);
            void IProcessorBase.OnComponentRemoved(IMarkedComponent comp) => OnComponentRemoved((TThis)comp);
        }
    }

    /// <summary>
    /// Dummy interface used to quickly filter out components that aren't marked for processing, do not use.
    /// </summary>
    public interface IMarkedComponent;
}
