// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.ExecServer
{
    /// <summary>
    /// Main server ServiceWire interface
    /// </summary>
    public interface IExecServerRemote
    {
        void Check();

        int Run(string currentDirectory, Dictionary<string, string> environmentVariables, string[] args, bool shadowCache, int? debuggerProcessId, string callbackAddress);
    }
}
