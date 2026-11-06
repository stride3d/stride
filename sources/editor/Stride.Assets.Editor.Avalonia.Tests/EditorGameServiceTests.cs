// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Headless.XUnit;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.EditorGame;
using Stride.Core.Assets;
using Stride.Core.Reflection;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Tests;

public sealed class EditorGameServiceTests
{
    [AvaloniaFact]
    public void TheSharedMousePositionServiceIsCreatedForEveryEditorGame()
    {
        var type = typeof(EditorGameMousePositionService);
        var services = AssetRegistry.AssetAssemblies.SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(EditorGameServiceAttribute))).Distinct().ToList();

        // The editor games create the services of the assets assemblies: the shared one is the only pointer position service,
        // it reads the pointer from the viewport of the game, which the Avalonia view fills
        Assert.Contains(type.Assembly, AssetRegistry.AssetAssemblies);
        Assert.Equal([type], services.Where(x => typeof(IEditorGameMousePositionViewModelService).IsAssignableFrom(x)));
        Assert.Equal(typeof(IEditorGameController), type.GetCustomAttribute<EditorGameServiceAttribute>()!.ControllerType);
        // The scene editor asks the controller for it to place the entities of the assets dropped on the view
        Assert.True(typeof(IEditorGameViewModelService).IsAssignableFrom(type));
    }
}
