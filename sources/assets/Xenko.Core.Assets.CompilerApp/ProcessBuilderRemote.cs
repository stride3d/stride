// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.BuildEngine;
using Xenko.Core.Diagnostics;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Storage;

namespace Xenko.Core.Assets.CompilerApp
{
    public class ProcessBuilderRemote : IProcessBuilderRemote
    {
        private readonly AssemblyContainer assemblyContainer;
        private readonly LocalCommandContext commandContext;
        private readonly Command remoteCommand;

        public CommandResultEntry Result { get; protected set; }

        public ProcessBuilderRemote(AssemblyContainer assemblyContainer, LocalCommandContext commandContext, Command remoteCommand)
        {
            this.assemblyContainer = assemblyContainer;
            this.commandContext = commandContext;
            this.remoteCommand = remoteCommand;
        }

        public Command GetCommandToExecute()
        {
            return remoteCommand;
        }

        public void RegisterResult(CommandResultEntry commandResult)
        {
            Result = commandResult;
        }

        public void ForwardLog(SerializableLogMessage message)
        {
            commandContext.Logger.Log(new LogMessage(message.Module, message.Type, message.Text));
            if (message.ExceptionInfo != null)
                commandContext.Logger.Log(new LogMessage(message.Module, message.Type, message.ExceptionInfo.ToString()));
        }

        public ObjectId ComputeInputHash(UrlType type, string filePath)
        {
            return commandContext.ComputeInputHash(type, filePath);
        }

        public Dictionary<ObjectUrl, ObjectId> GetOutputObjects()
        {
            var result = new Dictionary<ObjectUrl, ObjectId>();
            foreach (var outputObjects in commandContext.GetOutputObjectsGroups())
            {
                foreach (var outputObject in outputObjects)
                {
                    if (!result.ContainsKey(outputObject.Key))
                    {
                        result.Add(outputObject.Key, outputObject.Value.ObjectId);
                    }
                }
            }
            return result;
        }

        public List<string> GetAssemblyContainerLoadedAssemblies()
        {
            return assemblyContainer.LoadedAssemblies.Select(x => x.Path).ToList();
        }
    }
}
