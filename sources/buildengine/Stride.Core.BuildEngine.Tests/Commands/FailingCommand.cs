// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

namespace Stride.Core.BuildEngine.Tests.Commands
{
    public class FailingCommand : TestCommand
    {
        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            return Task.FromResult(ResultStatus.Failed);
        }
    }
}
