// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Headless.XUnit;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.Avalonia.EditorGame;
using Stride.Core.Assets;
using Stride.Core.Reflection;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Tests;

public sealed class EditorGameServiceTests
{
    [AvaloniaFact]
    public void TheMousePositionServiceIsCreatedForEveryEditorGame()
    {
        var type = typeof(AvaloniaEditorGameMousePositionService);

        // The editor games create the services of the assets assemblies: this assembly is one, as the WPF assets editor
        Assert.Contains(type.Assembly, AssetRegistry.AssetAssemblies);
        Assert.Contains(type, AssemblyRegistry.GetScanTypes(type.Assembly, typeof(EditorGameServiceAttribute)));
        Assert.Equal(typeof(IEditorGameController), type.GetCustomAttribute<EditorGameServiceAttribute>()!.ControllerType);
        // The scene editor asks the controller for it to place the entities of the assets dropped on the view
        Assert.True(typeof(IEditorGameMousePositionViewModelService).IsAssignableFrom(type));
        Assert.True(typeof(IEditorGameViewModelService).IsAssignableFrom(type));
    }
}
