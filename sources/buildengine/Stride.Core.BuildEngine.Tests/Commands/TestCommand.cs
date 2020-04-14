// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;

using Stride.Core.Serialization;

namespace Stride.Core.BuildEngine.Tests.Commands
{
    public abstract class TestCommand : Command
    {
        /// <inheritdoc/>
        public override string Title { get { return ToString(); } }

        private static int commandCounter;
        private readonly int commandId;

        public static void ResetCounter()
        {
            commandCounter = 0;
        }

        protected TestCommand()
        {
            commandId = ++commandCounter;
        }

        public override string ToString()
        {
            return GetType().Name + " " + commandId;
        }

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);

            writer.Write(commandId);
        }
    }
}
