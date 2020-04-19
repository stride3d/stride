// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Storage;
using System.Threading.Tasks;

using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.BuildEngine
{
    public abstract class CommandContextBase : ICommandContext
    {
        public Command CurrentCommand { get; }

        public abstract LoggerResult Logger { get; }

        protected internal readonly CommandResultEntry ResultEntry;

        public abstract IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups();

        public abstract ObjectId ComputeInputHash(UrlType type, string filePath);

        protected CommandContextBase(Command command, BuilderContext builderContext)
        {
            CurrentCommand = command;
            ResultEntry = new CommandResultEntry();
        }

        public void RegisterInputDependency(ObjectUrl url)
        {
            ResultEntry.InputDependencyVersions.Add(url, ComputeInputHash(url.Type, url.Path));
        }

        public void RegisterOutput(ObjectUrl url, ObjectId hash)
        {
            ResultEntry.OutputObjects.Add(url, hash);
        }

        public void RegisterCommandLog(IEnumerable<ILogMessage> logMessages)
        {
            foreach (var message in logMessages)
            {
                ResultEntry.LogMessages.Add(message as SerializableLogMessage ?? new SerializableLogMessage((LogMessage)message));
            }
        }

        public void AddTag(ObjectUrl url, string tag)
        {
            ResultEntry.TagSymbols.Add(new KeyValuePair<ObjectUrl, string>(url, tag));
        }


    }
}
