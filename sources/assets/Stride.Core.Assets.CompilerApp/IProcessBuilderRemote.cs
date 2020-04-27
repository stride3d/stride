// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Core.Assets.CompilerApp
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
