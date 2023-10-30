// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

public sealed class EditorDebugService : IEditorDebugService
{
    private static readonly List<IDebugPage> debugPages = [];
    private static readonly HashSet<DebugWindowViewModel> debugWindows = [];

    private readonly IViewModelServiceProvider serviceProvider;

    public EditorDebugService(IViewModelServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    private IDispatcherService Dispatcher => serviceProvider.Get<IDispatcherService>();

    public IDebugPage CreateAssetNodesDebugPage(ISessionViewModel session, string title, bool register = true)
    {
        return Dispatcher.Invoke(() =>
        {
            var page = new DebugAssetNodeCollectionViewModel(session) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        });
    }

    public IDebugPage CreateLogDebugPage(Logger logger, string title, bool register = true)
    {
        // Activate all log levels
        logger.ActivateLog(LogMessageType.Debug);

        return Dispatcher.Invoke(() =>
        {
            var page = new LoggerViewModel(serviceProvider, logger) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        });
    }

    public IDebugPage CreateUndoRedoDebugPage(IUndoRedoService actionService, string title, bool register = true)
    {
        return Dispatcher.Invoke(() =>
        {
            var page = new UndoRedoViewModel(serviceProvider, actionService) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        });
    }

    public void RegisterDebugPage(IDebugPage? page)
    {
        if (page is null) return;
        Dispatcher.CheckAccess();

        debugPages.Add(page);
        foreach (var debugWindow in debugWindows)
        {
            debugWindow.Pages.Add(page);
        }
    }

    public void UnregisterDebugPage(IDebugPage? page)
    {
        if (page is null) return;
        Dispatcher.CheckAccess();

        debugPages.Remove(page);
        foreach (var debugWindow in debugWindows)
        {
            debugWindow.Pages.Remove(page);
        }
        (page as IDestroyable)?.Destroy();
    }

    public static void RegisterDebugWindow(DebugWindowViewModel debugWindow)
    {
        if (debugWindows.Add(debugWindow))
        {
            debugWindow.Pages.AddRange(debugPages);
        }
    }

    public static void UnregisterDebugWindow(DebugWindowViewModel debugWindow)
    {
        if (debugWindows.Remove(debugWindow))
        {
            debugWindow.Pages.Clear();
        }
    }
}
