// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.Components.DebugTools.UndoRedo.Views;
using Xenko.Core.Assets.Editor.View.DebugTools;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Services
{
    public interface IDebugPage
    {
        string Title { get; set; }
    }

    public static class EditorDebugTools
    {
        internal static readonly List<IDebugPage> DebugPages = new List<IDebugPage>();
        private static readonly List<DebugWindow> DebugWindows = new List<DebugWindow>();

        public static void RegisterDebugPage(IDebugPage page)
        {
            lock (DebugPages)
            {
                DebugPages.Add(page);
                foreach (var debugWindow in DebugWindows)
                {
                    debugWindow.Pages.Add(page);
                }
            }
        }

        public static void UnregisterDebugPage(IDebugPage page)
        {
            lock (DebugPages)
            {
                DebugPages.Remove(page);
                foreach (var debugWindow in DebugWindows)
                {
                    debugWindow.Pages.Remove(page);
                }
                var disposable = page as IDestroyable;
                disposable?.Destroy();
            }
        }

        public static IDebugPage CreateLogDebugPage(Logger logger, string title, bool register = true)
        {
            var dispatcher = SessionViewModel.Instance.ServiceProvider.Get<IDispatcherService>();
            dispatcher.EnsureAccess();
            // Activate all log levels
            logger.ActivateLog(LogMessageType.Debug);
            var loggerViewModel = new LoggerViewModel(SessionViewModel.Instance.ServiceProvider, logger);
            var page = new DebugLogUserControl(loggerViewModel) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        }

        public static IDebugPage CreateUndoRedoDebugPage(IUndoRedoService service, string title, bool register = true)
        {
            var dispatcher = SessionViewModel.Instance.ServiceProvider.Get<IDispatcherService>();
            dispatcher.EnsureAccess();
            var page = new DebugUndoRedoUserControl(SessionViewModel.Instance.ServiceProvider, service) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        }

        public static IDebugPage CreateAssetNodesDebugPage(SessionViewModel session, string title, bool register = true)
        {
            session.Dispatcher.EnsureAccess();
            var page = new DebugAssetNodesUserControl(session) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        }

        internal static void RegisterDebugWindow(DebugWindow debugWindow)
        {
            lock (DebugPages)
            {
                DebugWindows.Add(debugWindow);
                debugWindow.Pages.AddRange(DebugPages);
            }
        }

        internal static void UnregisterDebugWindow(DebugWindow debugWindow)
        {
            lock (DebugPages)
            {
                debugWindow.Pages.Clear();
                DebugWindows.Remove(debugWindow);
            }
        }
    }
}
