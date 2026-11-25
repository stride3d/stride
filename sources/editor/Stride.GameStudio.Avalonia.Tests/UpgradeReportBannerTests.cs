// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Core.Assets;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The banner of the main view that shows while a project of the session has an upgrade-report.md.
/// </summary>
public sealed class UpgradeReportBannerTests
{
    [AvaloniaFact]
    public async Task TheBannerShowsWhileAProjectHasAnUpgradeReport()
    {
        using var fixture = ShellSessionFixture.Create();
        // A report left by an earlier upgrade
        var report = Path.Combine(fixture.PackageDirectory, UpgradeReportWriter.FileName);
        File.WriteAllText(report, "# Upgrade report: Game.csproj\n");
        await fixture.OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var banner = window.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "UpgradeReportBanner");
            await WindowTestHelper.PumpAsync();
            Assert.True(banner.IsVisible);
            Assert.Equal([report], fixture.Main.UpgradeReportBanner.Reports);
            Assert.Contains("left items to check by hand", fixture.Main.UpgradeReportBanner.Message, StringComparison.Ordinal);

            // Deleting the file means its items are handled
            File.Delete(report);
            Assert.True(await WaitAsync(() => !banner.IsVisible));

            File.WriteAllText(report, "# Upgrade report: Game.csproj\n");
            Assert.True(await WaitAsync(() => banner.IsVisible));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task TheBannerOffersToFinishAnUnfinishedUpgrade()
    {
        using var fixture = ShellSessionFixture.Create();
        // An upgrade kept unfinished, with a step left
        var project = Directory.GetFiles(fixture.PackageDirectory, "*.csproj").Single();
        var document = XDocument.Load(project);
        document.Root!.Add(new XElement("ItemGroup", new XElement(PackageUpgrader.UpgradeRecordItem,
            new XAttribute("Include", "Stride"), new XAttribute("Version", "4.5.5"), new XAttribute(PackageUpgrader.UnfinishedMetadata, "Stride-4.5.0-P2"))));
        document.Save(project);
        await fixture.OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var banner = window.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "UpgradeReportBanner");
            Button Button(string name) => banner.GetVisualDescendants().OfType<Button>().Single(x => x.Name == name);
            Assert.True(await WaitAsync(() => banner.IsVisible && fixture.Main.UpgradeReportBanner.IsUnfinished));
            Assert.Contains("is unfinished, with 1 step(s) left", fixture.Main.UpgradeReportBanner.Message, StringComparison.Ordinal);
            Assert.True(Button("RetryUpgradeButton").IsVisible);
            Assert.True(Button("MarkUpgradeFinishedButton").IsVisible);
            Assert.False(Button("RemoveUpgradeReportButton").IsVisible);

            // Marked as finished outside the editor, as 'stride upgrade --mark-finished' does
            VSProjectHelper.MarkUpgradesFinished(project);
            Assert.True(await WaitAsync(() => !banner.IsVisible));
        }
        finally
        {
            window.Close();
        }
    }

    // The watcher of the files reports on its own thread, then the banner updates on the UI thread
    private static async Task<bool> WaitAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(50);
            await WindowTestHelper.PumpAsync();
        }
        return condition();
    }
}
