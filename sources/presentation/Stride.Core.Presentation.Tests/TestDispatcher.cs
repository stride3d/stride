// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using Xunit;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.View;

namespace Xenko.Core.Presentation.Tests
{
    public class TestDispatcher
    {
        [Fact]
        public void TestInvoke()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            dispatcher.Invoke(() => count = 2);
            Assert.Equal(2, count);
            ShutdownDispatcher(dispatcher);
        }

        [Fact]
        public void TestInvokeResult()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            int result = dispatcher.Invoke(() => ++count);
            Assert.Equal(2, result);
            ShutdownDispatcher(dispatcher);
        }

        [Fact]
        public void TestInvokeAsyncFireAndForget()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            dispatcher.InvokeAsync(async () => { await Task.Delay(100); count = count + 1; }).Forget();
            Assert.Equal(1, count);
            Thread.Sleep(200);
            Assert.Equal(2, count);
            ShutdownDispatcher(dispatcher);
        }

        [Fact]
        public void TestInvokeTask()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            var task = dispatcher.InvokeAsync(async () => { await Task.Delay(100); count = count + 1; });
            Assert.Equal(1, count);
            task.Result.Wait();
            Assert.Equal(2, count);
            ShutdownDispatcher(dispatcher);
        }

        [Fact]
        public void TestInvokeTaskResult()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            var task = dispatcher.InvokeAsync(async () => { await Task.Delay(100); return 2; });
            Assert.Equal(1, count);
            task.Wait();
            count += task.Result.Result;
            Assert.Equal(3, count);
            ShutdownDispatcher(dispatcher);
        }
        
        [Fact]
        public async Task TestInvokeAsyncTask()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            var task = dispatcher.InvokeAsync(async () => { await Task.Delay(100); count = count + 1; });
            Assert.Equal(1, count);
            await task.Result;
            Assert.Equal(2, count);
            ShutdownDispatcher(dispatcher);
        }

        [Fact]
        public async Task TestInvokeAsyncTaskResult()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            var task = dispatcher.InvokeAsync(async () => { await Task.Delay(100); return 2; });
            Assert.Equal(1, count);
            count += await task.Result;
            Assert.Equal(3, count);
            ShutdownDispatcher(dispatcher);
        }

        static void ShutdownDispatcher(IDispatcherService dispatcher)
        {
            dispatcher.Invoke(() => Dispatcher.CurrentDispatcher.InvokeShutdown());
        }

        static IDispatcherService CreateDispatcher()
        {
            var initializationSignal = new AutoResetEvent(false);
            IDispatcherService result = null;
            var dispatcherThread = new Thread(() =>
            {
                result = DispatcherService.Create();
                initializationSignal.Set();
                Dispatcher.Run();
            });
            dispatcherThread.Start();
            initializationSignal.WaitOne();
            return result;
        }
    }
}
