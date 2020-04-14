// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ServiceModel;
using Stride.Core.Diagnostics;

namespace Stride.Debugger.Target
{
    /// <summary>
    /// Represents the debugger host commands that the target can access
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IGameDebuggerTarget))]
    public interface IGameDebuggerHost
    {
        [OperationContract]
        void RegisterTarget();

        [OperationContract]
        void OnGameExited();

        [OperationContract(IsOneWay = true)]
        void OnLogMessage(SerializableLogMessage logMessage);
    }
}
