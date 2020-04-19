// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stride.Core.BuildEngine.Tests.Commands
{
    [ContentSerializer(typeof(DataContentSerializer<DataContainer>))]
    [DataContract]
    [Serializable]
    public class DataContainer 
    {
        public byte[] Data;

        public static DataContainer Load(Stream stream)
        {
            return new DataContainer { Data = Utilities.ReadStream(stream) };
        }

        public DataContainer Alterate()
        {
            var result = new DataContainer { Data = new byte[Data.Length] };
            for (var i = 0; i < Data.Length; ++i)
            {
                unchecked { result.Data[i] = (byte)(Data[i] + 1); }
            }
            return result;
        }
    }

    public sealed class InputOutputTestCommand : IndexFileCommand
    {
        /// <inheritdoc/>
        public override string Title { get { return "InputOutputTestCommand " + Source + " > " + OutputUrl; } }

        public int Delay = 0;

        public Guid Id { get { throw new NotImplementedException(); } }
        public string Location => OutputUrl;

        public ObjectUrl Source;
        public string OutputUrl;
        public List<ObjectUrl> InputDependencies = new List<ObjectUrl>();

        public bool ExecuteRemotely = false;

        public List<Command> CommandsToSpawn = new List<Command>();

        public override string OutputLocation => Location;

        private async Task<bool> WaitDelay()
        {
            // Simulating actual work on input to generate output
            int nbSleep = Delay / 100;
            for (int i = 0; i < nbSleep; ++i)
            {
                await Task.Delay(100);
                if (CancellationToken.IsCancellationRequested)
                    return false;
            }

            await Task.Delay(Delay - (nbSleep * 100));
            return true;
        }

        public override IEnumerable<ObjectUrl> GetInputFiles()
        {
            yield return Source;
        }

        protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
            DataContainer result = null;

            switch (Source.Type)
            {
                case UrlType.File:
                    using (var fileStream = new FileStream(Source.Path, FileMode.Open, FileAccess.Read))
                    {
                        if (!await WaitDelay())
                            return ResultStatus.Cancelled;

                        result = DataContainer.Load(fileStream);
                    }
                    break;
                case UrlType.Content:
                    var container = assetManager.Load<DataContainer>(Source.Path);

                        if (!await WaitDelay())
                            return ResultStatus.Cancelled;

                     result = container.Alterate();
                  break;
            }

            assetManager.Save(OutputUrl, result);

            foreach (ObjectUrl inputDep in InputDependencies)
            {
                commandContext.RegisterInputDependency(inputDep);
            }
            return ResultStatus.Successful;
        }

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            writer.Write(Source);
            writer.Write(OutputUrl);
        }

        public override bool ShouldSpawnNewProcess()
        {
            return ExecuteRemotely;
        }

        public override string ToString()
        {
            return "InputOutputTestCommand " + Source + " > " + OutputUrl;
        }

    }
}
