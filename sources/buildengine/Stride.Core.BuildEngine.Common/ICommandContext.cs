// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Storage;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.BuildEngine
{
    public interface ICommandContext
    {
        Command CurrentCommand { get; }

        LoggerResult Logger { get; }

        IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups();

        void RegisterInputDependency(ObjectUrl url);

        void RegisterOutput(ObjectUrl url, ObjectId hash);

        void RegisterCommandLog(IEnumerable<ILogMessage> logMessages);

        void AddTag(ObjectUrl url, string tag);
    }
}
