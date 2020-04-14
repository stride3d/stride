// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Tests
{
    internal static class WindowManagerHelper
    {
        private const int TimeoutDelay = 10000;
        // This must remains a field to prevent garbage collection!
        private static TaskCompletionSource<int> nextWindowShown = new TaskCompletionSource<int>();
        private static TaskCompletionSource<int> nextWindowHidden = new TaskCompletionSource<int>();
        private static bool forwardingToConsole;

        public static Task Timeout => !Debugger.IsAttached ? Task.Delay(TimeoutDelay) : new TaskCompletionSource<int>().Task;

        public static Task NextWindowShown => nextWindowShown.Task;

        public static Task NextWindowHidden => nextWindowHidden.Task;

        public static Task<Dispatcher> CreateUIThread()
        {
            var tcs = new TaskCompletionSource<Dispatcher>();
            var thread = new Thread(() =>
            {
                tcs.SetResult(Dispatcher.CurrentDispatcher);
                Dispatcher.Run();
            })
            {
                Name = "Test UI thread",
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static void ShutdownUIThread(Dispatcher dispatcher)
        {
            Thread thread = null;
            dispatcher.Invoke(() => thread = Thread.CurrentThread);
            dispatcher.InvokeShutdown();
            thread.Join();
        }

        public static async Task TaskWithTimeout(Task task)
        {
            await Task.WhenAny(task, Timeout);
            if (task.Exception != null)
                ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();

            Assert.True(task.IsCompleted, "Test timed out");
        }

        public static LoggerResult CreateLoggerResult(Logger logger)
        {
            var loggerResult = new LoggerResult();
            logger.MessageLogged += (sender, e) => loggerResult.Log(e.Message);
            if (!forwardingToConsole)
            {
                logger.MessageLogged += (sender, e) => Console.WriteLine(e.Message);
                forwardingToConsole = true;
            }
            return loggerResult;
        }

        public static IDisposable InitWindowManager(Dispatcher dispatcher, out LoggerResult loggerResult)
        {
            var manager = new WindowManagerWrapper(dispatcher);
            loggerResult = CreateLoggerResult(WindowManager.Logger);
            return manager;
        }

        private class WindowManagerWrapper : IDisposable
        {
            private static Dispatcher uiDispatcher;
            private static NativeHelper.WinEventDelegate winEventProc;
            private static IntPtr hook;
            private readonly WindowManager manager;

            public WindowManagerWrapper(Dispatcher dispatcher)
            {
                uiDispatcher = dispatcher;
                manager = uiDispatcher.Invoke(() =>
                {
                    winEventProc = WinEventProc;
                    var processId = (uint)Process.GetCurrentProcess().Id;
                    hook = NativeHelper.SetWinEventHook(NativeHelper.EVENT_OBJECT_SHOW, NativeHelper.EVENT_OBJECT_HIDE, IntPtr.Zero, winEventProc, processId, 0, NativeHelper.WINEVENT_OUTOFCONTEXT);
                    if (hook == IntPtr.Zero) throw new InvalidOperationException("Unable to initialize the window manager.");
                    return new WindowManager(uiDispatcher);
                });
            }

            public void Dispose()
            {
                uiDispatcher.Invoke(() =>
                {
                    manager.Dispose();
                    if (!NativeHelper.UnhookWinEvent(hook)) throw new InvalidOperationException("An error occurred while disposing the window manager.");
                    hook = IntPtr.Zero;
                    winEventProc = null;
                });
            }

            private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                if (hwnd == IntPtr.Zero)
                    return;

                var rootHwnd = NativeHelper.GetAncestor(hwnd, NativeHelper.GetAncestorFlags.GetRoot);
                if (rootHwnd != IntPtr.Zero && rootHwnd != hwnd)
                    return;

                // idObject == 0 means it is the window itself, not a child object
                if (eventType == NativeHelper.EVENT_OBJECT_SHOW && idObject == 0)
                {
                    Assert.True(uiDispatcher.CheckAccess());
                    WindowShown(hwnd);
                }
                if (eventType == NativeHelper.EVENT_OBJECT_HIDE && idObject == 0)
                {
                    Assert.True(uiDispatcher.CheckAccess());
                    WindowHidden();
                }
            }

            private static void WindowShown(IntPtr hwnd)
            {
                if (!HwndHelper.HasStyleFlag(hwnd, NativeHelper.WS_VISIBLE))
                    return;

                var oldTcs = nextWindowShown;
                nextWindowShown = new TaskCompletionSource<int>();
                oldTcs.SetResult(0);
            }

            private static void WindowHidden()
            {
                var oldTcs = nextWindowHidden;
                nextWindowHidden = new TaskCompletionSource<int>();
                oldTcs.SetResult(0);
            }
        }
    }
}
