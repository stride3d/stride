// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
            Assert.Contains("left items to do by hand", fixture.Main.UpgradeReportBanner.Message, StringComparison.Ordinal);

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
