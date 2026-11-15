// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.Avalonia.EditorGame;
using Stride.Assets.Editor.EditorGame;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Reflection;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Tests;

public sealed class EditorGameServiceTests
{
    [AvaloniaFact]
    public void TheSharedMousePositionServiceIsCreatedForEveryEditorGame()
    {
        var type = typeof(EditorGameMousePositionService);

        // The editor games create the services of the assets assemblies: the shared one is the only pointer position service,
        // it reads the pointer from the viewport of the game, which the Avalonia view fills
        Assert.Contains(type.Assembly, AssetRegistry.AssetAssemblies);
        Assert.Equal([type], EditorGameServices().Where(x => typeof(IEditorGameMousePositionViewModelService).IsAssignableFrom(x)));
        Assert.Equal(typeof(IEditorGameController), type.GetCustomAttribute<EditorGameServiceAttribute>()!.ControllerType);
        // The scene editor asks the controller for it to place the entities of the assets dropped on the view
        Assert.True(typeof(IEditorGameViewModelService).IsAssignableFrom(type));
    }

    [AvaloniaFact]
    public void TheAvaloniaCursorServiceIsCreatedForEveryEditorGame()
    {
        var type = typeof(AvaloniaEditorGameCursorService);

        // The editor view assembly is an assets assembly: its cursor service serves the game of every editor
        Assert.Contains(type.Assembly, AssetRegistry.AssetAssemblies);
        Assert.Equal([type], EditorGameServices().Where(x => typeof(IEditorGameCursorViewModelService).IsAssignableFrom(x)));
        Assert.Equal(typeof(IEditorGameController), type.GetCustomAttribute<EditorGameServiceAttribute>()!.ControllerType);
    }

    [AvaloniaFact]
    public void TheSharedRecoveryServiceIsCreatedForEveryEditorGame()
    {
        var type = typeof(EditorGameRecoveryService);

        // An exception of the game of any editor pauses it and shows in the view of the editor
        Assert.Contains(type.Assembly, AssetRegistry.AssetAssemblies);
        Assert.Equal([type], EditorGameServices().Where(x => typeof(IEditorGameRecoveryViewModelService).IsAssignableFrom(x)));
        Assert.Equal(typeof(IEditorGameController), type.GetCustomAttribute<EditorGameServiceAttribute>()!.ControllerType);
    }

    [AvaloniaFact]
    public void TheCursorsTheGameAsksForShowOverTheView()
    {
        var viewport = new OffscreenGameViewport(new DispatcherService(Dispatcher.UIThread), new LoggerResult());
        var window = new Window { Width = 100, Height = 100, Content = viewport.View };
        window.Show();
        var service = new AvaloniaEditorGameCursorService(viewport);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(viewport.View.Cursor);

        // The adorners of the UI editor ask from the game thread: the cursor changes on the UI thread
        Task.Run(() => service.ChangeCursor(EditorGameCursor.SizeNS)).Wait();
        Dispatcher.UIThread.RunJobs();
        var sizing = viewport.View.Cursor;
        Assert.NotNull(sizing);

        // A burst of requests shows the last one; Default gives the view its default cursor back
        Task.Run(() =>
        {
            service.ChangeCursor(EditorGameCursor.SizeAll);
            service.ChangeCursor(EditorGameCursor.Default);
        }).Wait();
        Dispatcher.UIThread.RunJobs();
        Assert.Null(viewport.View.Cursor);

        // The drag distance is the platform's, once the view is shown
        var tapSize = viewport.View.GetPlatformSettings()!.GetTapSize(PointerType.Mouse);
        Assert.Equal(new Vector2((float)tapSize.Width, (float)tapSize.Height), service.MinimumDragDistance);

        window.Close();
        viewport.Dispose();
    }

    [AvaloniaTheory]
    [InlineData(EditorGameCursor.Default, null)]
    [InlineData(EditorGameCursor.No, StandardCursorType.No)]
    [InlineData(EditorGameCursor.SizeAll, StandardCursorType.SizeAll)]
    [InlineData(EditorGameCursor.SizeNS, StandardCursorType.SizeNorthSouth)]
    [InlineData(EditorGameCursor.SizeWE, StandardCursorType.SizeWestEast)]
    [InlineData(EditorGameCursor.SizeNWSE, StandardCursorType.TopLeftCorner)]
    [InlineData(EditorGameCursor.SizeNESW, StandardCursorType.TopRightCorner)]
    public void EachCursorOfTheGameHasItsAvaloniaCursor(EditorGameCursor cursor, StandardCursorType? expected)
    {
        Assert.Equal(expected, AvaloniaEditorGameCursorService.ToStandardCursor(cursor));
    }

    private static List<Type> EditorGameServices()
    {
        return AssetRegistry.AssetAssemblies.SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(EditorGameServiceAttribute))).Distinct().ToList();
    }
}
