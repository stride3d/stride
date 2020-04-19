// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading;
using System.Threading.Tasks;

namespace Stride.Core.BuildEngine.Tests.Commands
{
    public class DummyBlockingCommand : TestCommand
    {
        public int Delay = 0;

        protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            // Simulating actual work
            int nbSleep = Delay / 100;
            for (int i = 0; i < nbSleep; ++i)
            {
                Thread.Sleep(100);
                if (CancellationToken.IsCancellationRequested)
                    break;
            }
            if (!CancellationToken.IsCancellationRequested)
                Thread.Sleep(Delay - (nbSleep * 100));

            return await Task.FromResult(CancellationToken.IsCancellationRequested ? ResultStatus.Cancelled : ResultStatus.Successful);
        }
    }
}
