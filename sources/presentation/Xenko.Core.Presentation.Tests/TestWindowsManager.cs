// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NUnit.Framework;
using NUnitAsync;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Tests.WPF;
using Xenko.Core.Presentation.Windows;

namespace Xenko.Core.Presentation.Tests
{
    /// <summary>
    /// Test class for the <see cref="WindowManager"/>.
    /// </summary>
    /// <remarks>This class uses a monitor to run sequencially.</remarks>
    [TestFixture]
    public class TestWindowManager
    {
        private static readonly object LockObj = new object();
        private StaSynchronizationContext syncContext;

        [SetUp]
        protected virtual void Setup()
        {
            Monitor.Enter(LockObj);
            syncContext = new StaSynchronizationContext();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            syncContext.Dispose();
            syncContext = null;
            Monitor.Exit(LockObj);
        }

        [Test]
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

        [TestCase(Step.ShowMain, Step.HideMain, Step.ShowModal, Step.HideModal, Step.ShowBlocking, Step.HideBlocking, TestName = "ShowMain, HideMain, ShowModal, HideModal, ShowBlocking, HideBlocking")]
        [TestCase(Step.ShowMain, Step.HideMain, Step.ShowBlocking, Step.HideBlocking, Step.ShowModal, Step.HideModal, TestName = "ShowMain, HideMain, ShowBlocking, HideBlocking, ShowModal, HideModal")]
        [TestCase(Step.ShowMain, Step.HideMain, Step.ShowBlocking, Step.ShowModal, Step.HideBlocking, Step.HideModal, TestName = "ShowMain, HideMain, ShowBlocking, ShowModal, HideBlocking, HideModal")]
        [TestCase(Step.ShowMain, Step.HideMain, Step.ShowBlocking, Step.ShowModal, Step.HideModal, Step.HideBlocking, TestName = "ShowMain, HideMain, ShowBlocking, ShowModal, HideModal, HideBlocking")]
        [TestCase(Step.ShowMain, Step.HideMain, Step.ShowModal, Step.ShowBlocking, Step.HideModal, Step.HideBlocking, TestName = "ShowMain, HideMain, ShowModal, ShowBlocking, HideModal, HideBlocking")]
        [TestCase(Step.ShowMain, Step.HideMain, Step.ShowModal, Step.ShowBlocking, Step.HideBlocking, Step.HideModal, TestName = "ShowMain, HideMain, ShowModal, ShowBlocking, HideBlocking, HideModal")]
        [TestCase(Step.ShowModal, Step.HideModal, Step.ShowMain, Step.HideMain, Step.ShowBlocking, Step.HideBlocking, TestName = "ShowModal, HideModal, ShowMain, HideMain, ShowBlocking, HideBlocking")]
        [TestCase(Step.ShowModal, Step.HideModal, Step.ShowBlocking, Step.HideBlocking, Step.ShowMain, Step.HideMain, TestName = "ShowModal, HideModal, ShowBlocking, HideBlocking, ShowMain, HideMain")]
        [TestCase(Step.ShowModal, Step.HideModal, Step.ShowBlocking, Step.ShowMain, Step.HideBlocking, Step.HideMain, TestName = "ShowModal, HideModal, ShowBlocking, ShowMain, HideBlocking, HideMain")]
        [TestCase(Step.ShowModal, Step.HideModal, Step.ShowBlocking, Step.ShowMain, Step.HideMain, TestName = "ShowModal, HideModal, ShowBlocking, ShowMain, HideMain")] // NOTE: in this case Blocking is closed by Main.
        [TestCase(Step.ShowModal, Step.HideModal, Step.ShowMain, Step.ShowBlocking, Step.HideMain, TestName = "ShowModal, HideModal, ShowMain, ShowBlocking, HideMain")] // NOTE: in this case Blocking is closed by Main.
        [TestCase(Step.ShowModal, Step.HideModal, Step.ShowMain, Step.ShowBlocking, Step.HideBlocking, Step.HideMain, TestName = "ShowModal, HideModal, ShowMain, ShowBlocking, HideBlocking, HideMain")]
        [TestCase(Step.ShowBlocking, Step.HideBlocking, Step.ShowModal, Step.HideModal, Step.ShowMain, Step.HideMain, TestName = "ShowBlocking, HideBlocking, ShowModal, HideModal, ShowMain, HideMain")]
        [TestCase(Step.ShowBlocking, Step.HideBlocking, Step.ShowMain, Step.HideMain, Step.ShowModal, Step.HideModal, TestName = "ShowBlocking, HideBlocking, ShowMain, HideMain, ShowModal, HideModal")]
        [TestCase(Step.ShowBlocking, Step.HideBlocking, Step.ShowMain, Step.ShowModal, Step.HideMain, Step.HideModal, TestName = "ShowBlocking, HideBlocking, ShowMain, ShowModal, HideMain, HideModal")]
        [TestCase(Step.ShowBlocking, Step.HideBlocking, Step.ShowMain, Step.ShowModal, Step.HideModal, Step.HideMain, TestName = "ShowBlocking, HideBlocking, ShowMain, ShowModal, HideModal, HideMain")]
        [TestCase(Step.ShowBlocking, Step.HideBlocking, Step.ShowModal, Step.ShowMain, Step.HideModal, Step.HideMain, TestName = "ShowBlocking, HideBlocking, ShowModal, ShowMain, HideModal, HideMain")]
        [TestCase(Step.ShowBlocking, Step.HideBlocking, Step.ShowModal, Step.ShowMain, Step.HideMain, Step.HideModal, TestName = "ShowBlocking, HideBlocking, ShowModal, ShowMain, HideMain, HideModal")]
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
                            Assert.IsNull(mainWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowShown;
                            window = dispatcher.Invoke(() => new TestWindow("MainWindow"));
                            window.Shown += (sender, e) => { mainWindow = window; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => WindowManager.ShowMainWindow(window)).Task;
                            break;
                        case Step.ShowModal:
                            Assert.IsNull(modalWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowShown;
                            window = dispatcher.Invoke(() => new TestWindow("ModalWindow"));
                            window.Shown += (sender, e) => { modalWindow = window; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => window.ShowDialog()).Task;
                            break;
                        case Step.ShowBlocking:
                            Assert.IsNull(blockingWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowShown;
                            window = dispatcher.Invoke(() => new TestWindow("BlockingWindow"));
                            window.Shown += (sender, e) => { blockingWindow = window; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => WindowManager.ShowBlockingWindow(window)).Task;
                            break;
                        case Step.HideMain:
                            Assert.IsNotNull(mainWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowHidden;
                            window = mainWindow;
                            window.Closed += (sender, e) => { mainWindow = null; blockingWindow = null; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => window.Close()).Task;
                            break;
                        case Step.HideModal:
                            Assert.IsNotNull(modalWindow);
                            windowsManagerTask = WindowManagerHelper.NextWindowHidden;
                            window = modalWindow;
                            window.Closed += (sender, e) => { modalWindow = null; windowsEvent.SetResult(0); };
                            dispatcherTask = dispatcher.InvokeAsync(() => window.Close()).Task;
                            break;
                        case Step.HideBlocking:
                            Assert.IsNotNull(blockingWindow);
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
                    Assert.IsNotNull(mainWindow, step.ToString());
                    break;
                case Step.ShowModal:
                    Assert.IsNotNull(modalWindow, step.ToString());
                    break;
                case Step.ShowBlocking:
                    Assert.IsNotNull(blockingWindow, step.ToString());
                    break;
                case Step.HideMain:
                    Assert.IsNull(mainWindow, step.ToString());
                    break;
                case Step.HideModal:
                    Assert.IsNull(modalWindow, step.ToString());
                    break;
                case Step.HideBlocking:
                    Assert.IsNull(blockingWindow, step.ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step, null);
            }

            if (mainWindow != null)
            {
                var winInfo = WindowManager.MainWindow;
                Assert.NotNull(winInfo, step.ToString());
                Assert.AreEqual(mainWindow, winInfo.Window, step.ToString());
                Assert.True(winInfo.IsModal, step.ToString()); // TODO: should return false ideally;
                Assert.AreEqual(modalWindow != null || blockingWindow != null, winInfo.IsDisabled, step.ToString());
            }
            else
            {
                Assert.AreEqual(null, WindowManager.MainWindow, step.ToString());
            }
            if (modalWindow != null)
            {
                Assert.AreEqual(1, WindowManager.ModalWindows.Count, step.ToString());
                var winInfo = WindowManager.ModalWindows[0];
                Assert.AreEqual(modalWindow, winInfo.Window, step.ToString());
                Assert.True(winInfo.IsModal, step.ToString());
                Assert.False(winInfo.IsDisabled, step.ToString());
            }
            else
            {
                Assert.AreEqual(0, WindowManager.ModalWindows.Count, step.ToString());
            }
            if (blockingWindow != null)
            {
                Assert.AreEqual(1, WindowManager.BlockingWindows.Count, step.ToString());
                var winInfo = WindowManager.BlockingWindows[0];
                Assert.AreEqual(blockingWindow, winInfo.Window, step.ToString());
                Assert.False(winInfo.IsModal, step.ToString());
                Assert.AreEqual(modalWindow != null, winInfo.IsDisabled, step.ToString());
                Assert.AreEqual(mainWindow, winInfo.Owner?.Window, step.ToString());
            }
            else
            {
                Assert.AreEqual(0, WindowManager.BlockingWindows.Count, step.ToString());
            }
        }
    }
}
