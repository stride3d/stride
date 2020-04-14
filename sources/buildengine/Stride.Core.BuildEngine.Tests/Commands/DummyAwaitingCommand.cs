// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

namespace Stride.Core.BuildEngine.Tests.Commands
{
    public class DummyAwaitingCommand : TestCommand
    {
        public int Delay = 0;

        protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            // Simulating awaiting result
            try
            {
                await Task.Delay(Delay, CancellationToken);

            }
            catch (TaskCanceledException) {}

            return CancellationToken.IsCancellationRequested ? ResultStatus.Cancelled : ResultStatus.Successful;
        }
    }
}
