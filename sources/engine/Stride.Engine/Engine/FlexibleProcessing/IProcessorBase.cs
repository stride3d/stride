// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine.Design;

namespace Stride.Engine.FlexibleProcessing
{
    public interface IProcessorBase
    {
        /// <summary> The logic using this value expects it to be constant at runtime </summary>
        ExecutionMode ExecutionMode => ExecutionMode.Runtime;
        /// <summary> Occurs right after the first component handled by this processor is added to the scene, but before <see cref="OnComponentAdded"/> </summary>
        void SystemAdded(IServiceRegistry registryParam);
        /// <summary> Occurs right after the last component handled by this processor is removed from the scene, after the call to <see cref="OnComponentRemoved"/> </summary>
        void SystemRemoved();
        /// <summary> Occurs right after a component is added to the scene </summary>
        void OnComponentAdded(IMarkedComponent comp);
        /// <summary> Occurs right after a component is removed from the scene </summary>
        void OnComponentRemoved(IMarkedComponent comp);
    }
}
