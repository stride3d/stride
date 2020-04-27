// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Storage;
using Stride.Core.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.BuildEngine
{
    [DataContract(Inherited = true), Serializable]
    public abstract class Command
    {
        /// <summary>
        /// The command cache version, should be bumped when binary serialization format changes (so that cache gets invalidated)
        /// </summary>
        protected const int CommandCacheVersion = 1;

        /// <summary>
        /// Title (short description) of the command
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// The object this command writes (if any).
        /// </summary>
        public virtual string OutputLocation => null;

        /// <summary>
        /// Safeguard to ensure inheritance will always call base.PreCommand
        /// </summary>
        internal bool BasePreCommandCalled;

        /// <summary>
        /// Safeguard to ensure inheritance will always call base.PostCommand
        /// </summary>
        internal bool BasePostCommandCalled;

        /// <summary>
        /// Cancellation Token. Must be checked frequently by the <see cref="DoCommandOverride"/> implementation in order to interrupt the command while running
        /// </summary>
        public CancellationToken CancellationToken;

        /// <summary>
        /// The method to override containing the actual command code. It is called by the <see cref="DoCommand"/> function
        /// </summary>
        /// <param name="commandContext"></param>
        protected abstract Task<ResultStatus> DoCommandOverride(ICommandContext commandContext);

        /// <summary>
        /// The method that indirectly call <see cref="DoCommandOverride"/> to execute the actual command code. 
        /// It is called by the current <see cref="Builder"/> when the command is triggered
        /// </summary>
        /// <param name="commandContext"></param>
        public Task<ResultStatus> DoCommand(ICommandContext commandContext)
        {
            if (CancellationToken.IsCancellationRequested)
                return Task.FromResult(ResultStatus.Cancelled);

            return DoCommandOverride(commandContext);
        }

        public virtual void PreCommand(ICommandContext commandContext)
        {
            // Safeguard, will throw an exception if a inherited command does not call base.PreCommand
            BasePreCommandCalled = true;
        }

        public virtual void PostCommand(ICommandContext commandContext, ResultStatus status)
        {
            // Safeguard, will throw an exception if a inherited command does not call base.PostCommand
            BasePostCommandCalled = true;

            commandContext.RegisterCommandLog(commandContext.Logger.Messages);
        }

        public Command Clone()
        {
            var copy = (Command)Activator.CreateInstance(GetType());
            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetSetMethod() != null)
                {
                    var value = property.GetValue(this);
                    property.SetValue(copy, value);
                }
            }
            return copy;
        }

        /// <inheritdoc/>
        public abstract override string ToString();

        /// <summary>
        /// Gets the list of input files (that can be deduced without running the command, only from command parameters).
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<ObjectUrl> GetInputFiles()
        {
            return InputFilesGetter?.Invoke() ?? Enumerable.Empty<ObjectUrl>();
        }

        public Func<IEnumerable<ObjectUrl>> InputFilesGetter;

        /// <summary>
        /// Check some conditions that determine if the command should be executed. This method may not be called if some previous check determinated that it already needs to be executed.
        /// </summary>
        /// <returns>true if the command should be executed</returns>
        public virtual bool ShouldForceExecution()
        {
            return false;
        }

        public virtual bool ShouldSpawnNewProcess()
        {
            return false;
        }

        /// <summary>
        /// Callback called by <see cref="Builder.CancelBuild"/>. It can be useful for commands in a blocking call that can be unblocked from here.
        /// </summary>
        public virtual void Cancel()
        {
            // Do nothing by default
        }

        protected virtual void ComputeParameterHash(BinarySerializationWriter writer)
        {
            // Do nothing by default
        }

        protected void ComputeInputFilesHash(BinarySerializationWriter writer, IPrepareContext prepareContext)
        {
            var inputFiles = GetInputFiles();
            if (inputFiles == null)
                return;

            foreach (var inputFile in inputFiles)
            {
                var hash = prepareContext.ComputeInputHash(inputFile.Type, inputFile.Path);
                if (hash == ObjectId.Empty)
                {
                    writer.NativeStream.WriteByte(0);
                }
                else
                {
                    writer.NativeStream.Write((byte[])hash, 0, ObjectId.HashSize);
                }
            }
        }

        public void ComputeCommandHash(Stream stream, IPrepareContext prepareContext)
        {
            var writer = new BinarySerializationWriter(stream) { Context = { SerializerSelector = SerializerSelector.AssetWithReuse } };

            writer.Write(CommandCacheVersion);

            // Compute assembly hash
            ComputeAssemblyHash(writer);

            // Compute parameters hash
            ComputeParameterHash(writer);

            // Compute static input files hash (parameter dependent)
            ComputeInputFilesHash(writer, prepareContext);
        }

        protected virtual void ComputeAssemblyHash(BinarySerializationWriter writer)
        {
            // Use binary format version (bumping it forces everything to be reevaluated)
            writer.Write(DataSerializer.BinaryFormatVersion);

            // Gets the hash of the assembly of the command
            //writer.Write(AssemblyHash.ComputeAssemblyHash(GetType().Assembly));
        }

        /// <summary>
        /// Computes the command hash. If an error occurred, the hash is <see cref="ObjectId.Empty"/>
        /// </summary>
        /// <param name="prepareContext">The prepare context.</param>
        /// <returns>Hash of the command.</returns>
        internal ObjectId ComputeCommandHash(IPrepareContext prepareContext)
        {
            var stream = new DigestStream(Stream.Null);
            try
            {
                ComputeCommandHash(stream, prepareContext);
                return stream.CurrentHash;
            }
            catch (Exception ex)
            {
                prepareContext.Logger.Error($"Unexpected error while computing the command hash for [{GetType().Name}].", ex);
            }
            return ObjectId.Empty;
        }
    }
}
