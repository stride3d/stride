// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ServiceModel;

namespace Xenko.ExecServer
{
    /// <summary>
    /// Main server interface
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IServerLogger), SessionMode = SessionMode.Required)]
    public interface IExecServerRemote
    {
        [OperationContract(IsInitiating =  true)]
        void Check();

        [OperationContract(IsTerminating = true)]
        int Run(string currentDirectory, Dictionary<string, string> environmentVariables, string[] args, bool shadowCache, int? debuggerProcessId);
    }
}
