// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Xunit;
using NUnitAsync;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Tests.WPF;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Tests
{
    /// <summary>
    /// Test class for the <see cref="WindowManager"/>. : IDisposable
    /// </summary>
    /// <remarks>This class uses a monitor to run sequencially.</remarks>
    public class TestWindowManager : IDisposable
    {
        private StaSynchronizationContext syncContext;

        public TestWindowManager()
        {
            syncContext = new StaSynchronizationContext();
        }

        public void Dispose()
        {
            syncContext.Dispose();
            syncContext = null;
        }

        [Fact]
        public async Task TestInitDistroy()
        {
            LoggerResult loggerResult;
            var dispatcher = await WindowManagerHelper.CreateUIThread();
            using (WindowManagerHelper.InitWindowManager(dispatcher, out loggerResult))
            {
            }
            WindowManagerHelper.ShutdownUIThread(dispatcher);
        }

        public enum Step
        {
            ShowMain,
            ShowModal,
            ShowBlocking,
            HideMain,
            HideModal,
            HideBlocking,
        }

        [Theory]
        [InlineData(Step.ShowMain, Step.HideMain, Step.ShowModal, Step.HideModal, Step.ShowBlocking, Step.HideBlocking)]
        [InlineData(Step.ShowMain, Step.HideMain, Step.ShowBlocking, Step.HideBlocking, Step.ShowModal, Step.HideModal)]
        [InlineData(Step.ShowMain, Step.HideMain, Step.ShowBlocking, Step.ShowModal, Step.HideBlocking, Step.HideModal)]
        [InlineData(Step.ShowMain, Step.HideMain, Step.ShowBlocking, Step.ShowModal, Step.HideModal, Step.HideBlocking)]
        [InlineData(Step.ShowMain, Step.HideMain, Step.ShowModal, Step.ShowBlocking, Step.HideModal, Step.HideBlocking)]
        [InlineData(Step.ShowMain, Step.HideMain, Step.ShowModal, Step.ShowBlocking, Step.HideBlocking, Step.HideModal)]
        [InlineData(Step.ShowModal, Step.HideModal, Step.ShowMain, Step.HideMain, Step.ShowBlocking, Step.HideBlocking)]
        [InlineData(Step.ShowModal, Step.HideModal, Step.ShowBlocking, Step.HideBlocking, Step.ShowMain, Step.HideMain)]
        [InlineData(Step.ShowModal, Step.HideModal, Step.ShowBlocking, Step.ShowMain, Step.HideBlocking, Step.HideMain)]
        [InlineData(Step.ShowModal, Step.HideModal, Step.ShowBlocking, Step.ShowMain, Step.HideMain)] // NOTE: in this case Blocking is closed by Main.
        [InlineData(Step.ShowModal, Step.HideModal, Step.ShowMain, Step.ShowBlocking, Step.HideMain)] // NOTE: in this case Blocking is closed by Main.
        [InlineData(Step.ShowModal, Step.HideModal, Step.ShowMain, Step.ShowBlocking, Step.HideBlocking, Step.HideMain)]
        [InlineData(Step.ShowBlocking, Step.HideBlocking, Step.ShowModal, Step.HideModal, Step.ShowMain, Step.HideMain)]
        [InlineData(Step.ShowBlocking, Step.HideBlocking, Step.ShowMain, Step.HideMain, Step.ShowModal, Step.HideModal)]
        [InlineData(Step.ShowBlocking, Step.HideBlocking, Step.ShowMain, Step.ShowModal, Step.HideMain, Step.HideModal)]
        [InlineData(Step.ShowBlocking, Step.HideBlocking, Step.ShowMain, Step.ShowModal, Step.HideModal, Step.HideMain)]
        [InlineData(Step.ShowBlocking, Step.HideBlocking, Step.ShowModal, Step.ShowMain, Step.HideModal, Step.HideMain)]
        [InlineData(Step.ShowBlocking, Step.HideBlocking, Step.ShowModal, Step.ShowMain, Step.HideMain, Step.HideModal)]
        public async Task TestBlockingWindow(params Step[] steps)
        {
            TestWindow mainWindow = null;
            TestWindow modalWindow = null;
            TestWindow blockingWindow = null;
            LoggerResult loggerResult;
            var dispatcher = await WindowManagerHelper.CreateUIThread();
            using (WindowManagerHelper.InitWindowManager(dispatcher, out loggerResult))
            {
                foreach (var step in steps)
                {
                    TestWindow window;
                    var windowsEvent = new TaskCompletionSource<int>();
                    Task windowsManagerTask;
                    Task dispatcherTask;
                    switch (step)
                    {
                        case Step.ShowMain:
                            Assert.Null(mainWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowShown;
                            window = dispatcher.Invoke(() => new TestWindow("MainWindow"));
                            window.Shown += (sender, e) => { mainWindow = window; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => WindowManager.ShowMainWindow(window)).Task;
                            break;
                        case Step.ShowModal:
                            Assert.Null(modalWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowShown;
                            window = dispatcher.Invoke(() => new TestWindow("ModalWindow"));
                            window.Shown += (sender, e) => { modalWindow = window; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => window.ShowDialog()).Task;
                            break;
                        case Step.ShowBlocking:
                            Assert.Null(blockingWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowShown;
                            window = dispatcher.Invoke(() => new TestWindow("BlockingWindow"));
                            window.Shown += (sender, e) => { blockingWindow = window; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => WindowManager.ShowBlockingWindow(window)).Task;
                            break;
                        case Step.HideMain:
                            Assert.NotNull(mainWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowHidden;
                            window = mainWindow;
                            window.Closed += (sender, e) => { mainWindow = null; blockingWindow = null; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => window.Close()).Task;
                            break;
                        case Step.HideModal:
                            Assert.NotNull(modalWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowHidden;
                            window = modalWindow;
                            window.Closed += (sender, e) => { modalWindow = null; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => window.Close()).Task;
                            break;
                        case Step.HideBlocking:
                            Assert.NotNull(blockingWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowHidden;
                            window = blockingWindow;
                            window.Closed += (sender, e) => { blockingWindow = null; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => window.Close()).Task;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    dispatcherTask.Forget();
                    await windowsEvent.Task;
                    await windowsManagerTask;
                    // Wait one more "frame" to be sure everything has been run.
                    await dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                    dispatcher.Invoke(() => AssertStep(step, mainWindow, modalWindow, blockingWindow));
                }
            }
            WindowManagerHelper.ShutdownUIThread(dispatcher);
        }

        private void AssertStep(Step step, Window mainWindow, Window modalWindow, Window blockingWindow)
        {
            switch (step)
            {
                case Step.ShowMain:
                    Assert.NotNull(mainWindow);
                    break;
                case Step.ShowModal:
                    Assert.NotNull(modalWindow);
                    break;
                case Step.ShowBlocking:
                    Assert.NotNull(blockingWindow);
                    break;
                case Step.HideMain:
                    Assert.Null(mainWindow);
                    break;
                case Step.HideModal:
                    Assert.Null(modalWindow);
                    break;
                case Step.HideBlocking:
                    Assert.Null(blockingWindow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step, null);
            }

            if (mainWindow != null)
            {
                var winInfo = WindowManager.MainWindow;
                Assert.NotNull(winInfo);
                Assert.Equal(mainWindow, winInfo.Window);
                Assert.True(winInfo.IsModal); // TODO: should return false ideally;
                Assert.Equal(modalWindow != null || blockingWindow != null, winInfo.IsDisabled);
            }
            else
            {
                Assert.Null(WindowManager.MainWindow);
            }
            if (modalWindow != null)
            {
                Assert.Equal(1, WindowManager.ModalWindows.Count);
                var winInfo = WindowManager.ModalWindows[0];
                Assert.Equal(modalWindow, winInfo.Window);
                Assert.True(winInfo.IsModal);
                Assert.False(winInfo.IsDisabled);
            }
            else
            {
                Assert.Equal(0, WindowManager.ModalWindows.Count);
            }
            if (blockingWindow != null)
            {
                Assert.Equal(1, WindowManager.BlockingWindows.Count);
                var winInfo = WindowManager.BlockingWindows[0];
                Assert.Equal(blockingWindow, winInfo.Window);
                Assert.False(winInfo.IsModal);
                Assert.Equal(modalWindow != null, winInfo.IsDisabled);
                Assert.Equal(mainWindow, winInfo.Owner?.Window);
            }
            else
            {
                Assert.Equal(0, WindowManager.BlockingWindows.Count);
            }
        }
    }
}
