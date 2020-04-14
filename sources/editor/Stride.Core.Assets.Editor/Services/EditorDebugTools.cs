// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Components.DebugTools.UndoRedo.Views;
using Stride.Core.Assets.Editor.View.DebugTools;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Services
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
