// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Diagnostics;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The panels that come forward by themselves.
/// </summary>
public sealed class PanelActivationTests
{
    [AvaloniaFact]
    public async Task TheAssetViewComesForwardWhenTheSolutionExplorerSelectsAFolder()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Folder/Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            // With an editor open, the asset view shares its group with the logs
            var sheet = fixture.Asset("Folder/Sheet");
            fixture.Session.AssetCollection.SelectAssets([sheet]);
            await WindowTestHelper.PumpAsync();
            fixture.Session.EditSelectedContentCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.EditorCollection.ActiveEditor is SpriteSheetEditorViewModel, "the editor");
            await WindowTestHelper.PumpAsync();
            var panels = fixture.Main.Panels;
            var assetView = panels.FindDockable(BuiltInPanels.AssetView)!;
            Assert.Same(assetView.Owner, panels.FindDockable(BuiltInPanels.AssetLog)!.Owner);
            panels.Show(BuiltInPanels.AssetLog);
            await WindowTestHelper.PumpAsync();
            Assert.False(assetView.ViewModel.IsShown);

            // A folder selected in the solution explorer
            var locations = fixture.Session.AssetCollection.SelectedLocations;
            locations.Clear();
            locations.Add(sheet.Directory);
            await WindowTestHelper.PumpAsync();
            Assert.True(assetView.ViewModel.IsShown);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheOutputComesForwardOnAnErrorOfTheBuild()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var log = fixture.Main.Build.BuildLog;
        var logger = new LoggerResult("Build");
        try
        {
            var panels = fixture.Main.Panels;
            var output = panels.FindDockable(BuiltInPanels.BuildLog)!;
            panels.Show(BuiltInPanels.References);
            await WindowTestHelper.PumpAsync();
            Assert.False(output.ViewModel.IsShown);

            log.AddLogger(logger);
            logger.Info("Building");
            logger.Warning("A warning");
            log.Flush();
            await WindowTestHelper.PumpAsync();
            Assert.Equal(2, log.Messages.Count);
            Assert.False(output.ViewModel.IsShown);

            logger.Error("The build failed");
            log.Flush();
            await WindowTestHelper.PumpAsync();
            Assert.True(output.ViewModel.IsShown);
        }
        finally
        {
            log.ClearLoggers();
            await window.CloseAsync();
        }
    }
}
