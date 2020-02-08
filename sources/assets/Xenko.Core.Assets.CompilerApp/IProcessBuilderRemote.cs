// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.BuildEngine;
using Xenko.Core.Diagnostics;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Storage;

namespace Xenko.Core.Assets.CompilerApp
{
    public interface IProcessBuilderRemote
    {
        Command GetCommandToExecute();

        void ForwardLog(SerializableLogMessage message);

        void RegisterResult(CommandResultEntry commandResult);

        ObjectId ComputeInputHash(UrlType type, string filePath);

        Dictionary<ObjectUrl, ObjectId> GetOutputObjects();

        List<string> GetAssemblyContainerLoadedAssemblies();
    }
}
