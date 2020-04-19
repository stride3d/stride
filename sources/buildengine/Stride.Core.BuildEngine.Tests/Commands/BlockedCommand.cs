// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading;
using System.Threading.Tasks;

namespace Stride.Core.BuildEngine.Tests.Commands
{
    public class BlockedCommand : TestCommand
    {
        private readonly Semaphore sem = new Semaphore(0, 1);

        protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            sem.WaitOne();
            return await Task.FromResult(CancellationToken.IsCancellationRequested ? ResultStatus.Cancelled : ResultStatus.Successful);
        }

        public override void Cancel()
        {
            sem.Release();
        }
    }
}
