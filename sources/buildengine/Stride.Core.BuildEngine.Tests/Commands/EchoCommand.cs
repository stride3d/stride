// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.BuildEngine.Tests.Commands
{
    public class EchoCommand : TestCommand
    {
        public string InputUrl { get; set; }
        public string Echo { get; set; }

        public EchoCommand(string inputUrl, string echo)
        {
            InputUrl = inputUrl;
            Echo = echo;
        }

        public override IEnumerable<ObjectUrl> GetInputFiles()
        {
            yield return new ObjectUrl(UrlType.File, InputUrl);
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            Console.WriteLine(@"{0}: {1}", InputUrl, Echo);
            return Task.FromResult(ResultStatus.Successful);
        }
    }
}
