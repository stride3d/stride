// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Diagnostics;

namespace Xenko.Debugger.Target
{
    /// <summary>
    /// Represents the debugger host commands that the target can access
    /// </summary>
    public interface IGameDebuggerHost : IDisposable
    {
        void RegisterTarget(string callbackAddress);

        void OnGameExited();

        void OnLogMessage(SerializableLogMessage logMessage);
    }
}
