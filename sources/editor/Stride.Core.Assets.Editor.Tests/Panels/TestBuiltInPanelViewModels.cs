// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests.Panels;

public sealed class TestBuiltInPanelViewModels
{
    [Fact]
    public async Task ThePanelsOfTheSessionShowItsParts()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync();
        var build = new BuildAndRunViewModel(fixture.ServiceProvider);
        var panels = BuiltInPanels.CreateSessionPanels().Append(BuiltInPanels.CreateOutputPanel(build)).ToDictionary(x => x.Id);
        IPanelViewModel Create(string id) => panels[id].CreateViewModel(new PanelContext { Descriptor = panels[id], Session = fixture.Session, ServiceProvider = fixture.ServiceProvider });

        var explorer = Assert.IsType<SolutionExplorerPanelViewModel>(Create(BuiltInPanels.SolutionExplorer));
        Assert.Same(fixture.Session, explorer.Session);
        Assert.Equal(BuiltInPanels.SolutionExplorer, explorer.PanelId);
        Assert.Equal("", explorer.InstanceId);
        Assert.Equal("Solution explorer", explorer.Title);
        Assert.Same(fixture.Session, Assert.IsType<AssetViewPanelViewModel>(Create(BuiltInPanels.AssetView)).Session);
        Assert.Same(fixture.Session, Assert.IsType<PropertyGridPanelViewModel>(Create(BuiltInPanels.PropertyGrid)).Session);
        Assert.Same(fixture.Session.References, Assert.IsType<ReferencesPanelViewModel>(Create(BuiltInPanels.References)).References);
        Assert.Same(fixture.Session.ActionHistory, Assert.IsType<ActionHistoryPanelViewModel>(Create(BuiltInPanels.ActionHistory)).ActionHistory);

        // The asset errors have their count in the title
        var assetLog = Assert.IsType<AssetLogPanelViewModel>(Create(BuiltInPanels.AssetLog));
        Assert.Equal("Asset errors (0)", assetLog.Title);
        fixture.Session.AssetLog.ErrorCount = 1;
        Assert.Equal("Asset error (1)", assetLog.Title);
        fixture.Session.AssetLog.ErrorCount = 3;
        Assert.Equal("Asset errors (3)", assetLog.Title);
        assetLog.Destroy();
        fixture.Session.AssetLog.ErrorCount = 0;
        Assert.Equal("Asset errors (3)", assetLog.Title);

        // The Output: its messages are seen while it is shown, and its title says when some were not
        var output = Assert.IsType<OutputPanelViewModel>(Create(BuiltInPanels.BuildLog));
        Assert.Equal("Output", output.Title);
        output.IsShown = true;
        Assert.True(build.IsOutputVisible);
        output.IsShown = false;
        Assert.False(build.IsOutputVisible);
        var logger = new LoggerResult();
        build.BuildLog.AddLogger(logger);
        logger.Info("Compiling");
        build.BuildLog.Flush();
        Assert.Equal("Output*", output.Title);
        output.IsShown = true;
        Assert.Equal("Output", output.Title);
        output.Destroy();
        build.BuildLog.RemoveLogger(logger);
        build.Destroy();
    }
}
