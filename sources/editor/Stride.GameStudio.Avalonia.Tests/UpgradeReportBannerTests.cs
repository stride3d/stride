// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Core.Assets;
using Stride.Core.Presentation.Commands;
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
            // Without a backup, nothing puts the files back
            Assert.False(Button("RevertUpgradeButton").IsVisible);

            // Only a code step is left, which a retry doesn't run
            document = XDocument.Load(project);
            document.Descendants(PackageUpgrader.UpgradeRecordItem).Single().SetAttributeValue(PackageUpgrader.UnfinishedMetadata, "Stride-4.5.0-C3");
            document.Save(project);
            Assert.True(await WaitAsync(() => !Button("RetryUpgradeButton").IsVisible));
            Assert.Contains("left that don't run again", fixture.Main.UpgradeReportBanner.Message, StringComparison.Ordinal);
            Assert.True(Button("MarkUpgradeFinishedButton").IsVisible);

            // The upgrade has a backup to put the files back from
            var backup = new UpgradeBackup(fixture.PackageDirectory, DateTime.Now);
            backup.Snapshot(project);
            VSProjectHelper.SetUpgradeRecord(project, "Stride", "4.5.5", unfinished: ["Stride-4.5.0-C3"], backupFolder: backup.FolderPath);
            Assert.True(await WaitAsync(() => Button("RevertUpgradeButton").IsVisible));

            // Marked as finished outside the editor, as 'stride upgrade --mark-finished' does: without a report, the backup is still
            // offered until it is removed, as a revert would put it back
            PackageSession.MarkUpgradesFinished([project], new Stride.Core.Diagnostics.LoggerResult());
            Assert.True(await WaitAsync(() => !fixture.Main.UpgradeReportBanner.IsUnfinished));
            Assert.True(banner.IsVisible);
            Assert.Contains("keeps a backup", fixture.Main.UpgradeReportBanner.Message, StringComparison.Ordinal);
            Assert.True(Button("RevertUpgradeButton").IsVisible);
            Assert.False(Button("OpenUpgradeReportButton").IsVisible);
            Assert.Equal("Remove the backup", Button("RemoveUpgradeReportButton").Content);

            // With a report, the report is removed with the backup
            File.WriteAllText(Path.Combine(fixture.PackageDirectory, UpgradeReportWriter.FileName), "# Upgrade report: Game.csproj\n");
            Assert.True(await WaitAsync(() => Button("OpenUpgradeReportButton").IsVisible));
            Assert.Equal("Remove the report and backup", Button("RemoveUpgradeReportButton").Content);

            // In another clone, the records name a backup folder that version control doesn't have
            Directory.Delete(backup.FolderPath, recursive: true);
            File.WriteAllText(Path.Combine(fixture.PackageDirectory, UpgradeReportWriter.FileName), "# Upgrade report: Game.csproj\n\nTouched.\n");
            Assert.True(await WaitAsync(() => !Button("RevertUpgradeButton").IsVisible));
            Assert.Equal("Remove the report", Button("RemoveUpgradeReportButton").Content);
            File.Delete(Path.Combine(fixture.PackageDirectory, UpgradeReportWriter.FileName));
            Assert.True(await WaitAsync(() => !banner.IsVisible));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task RetryRunsTheStepsLeftAgain()
    {
        using var fixture = ShellSessionFixture.Create();
        // A project step left that changes nothing here: the project has no asset of a plugin package
        var project = WriteUnfinishedRecord(fixture, "Stride-4.5.0-P2");
        await fixture.OpenAsync();
        var dialogs = fixture.ReplaceDialogService();
        var banner = fixture.Main.UpgradeReportBanner;
        var reloads = 0;
        banner.ReloadCommand = new AnonymousCommand(fixture.ServiceProvider, () => reloads++);
        Assert.True(await WaitAsync(() => banner.CanRetry));

        banner.RetryCommand.Execute();

        Assert.True(await WaitAsync(() => dialogs.Messages.Count > 0));
        Assert.Contains("The steps left are done", dialogs.Messages.Single(), StringComparison.Ordinal);
        Assert.Empty(VSProjectHelper.GetUpgradeRecords(project).Unfinished);
        Assert.True(await WaitAsync(() => !banner.IsVisible));
        // The step changed no file: the session doesn't open again
        Assert.Equal(0, reloads);
    }

    [AvaloniaFact]
    public async Task RevertPutsBackTheFilesThenOpensTheSessionAgain()
    {
        using var fixture = ShellSessionFixture.Create();
        var project = Directory.GetFiles(fixture.PackageDirectory, "*.csproj").Single();
        var before = File.ReadAllText(project);
        var script = Path.Combine(fixture.PackageDirectory, "Game.cs");
        File.WriteAllText(script, "Before");
        // The upgrade changed the script and recorded its backup
        var backup = new UpgradeBackup(fixture.PackageDirectory, DateTime.Now);
        backup.Snapshot(project);
        backup.Snapshot(script);
        File.WriteAllText(script, "After");
        VSProjectHelper.SetUpgradeRecord(project, "Stride", "4.5.5", unfinished: ["Stride-4.5.0-C3"], backupFolder: backup.FolderPath);
        backup.RecordState();
        await fixture.OpenAsync();
        var dialogs = fixture.ReplaceDialogService();
        var banner = fixture.Main.UpgradeReportBanner;
        var reloads = 0;
        banner.ReloadCommand = new AnonymousCommand(fixture.ServiceProvider, () => reloads++);
        Assert.True(await WaitAsync(() => banner.CanRevert));

        banner.RevertCommand.Execute();

        Assert.True(await WaitAsync(() => reloads == 1));
        Assert.Contains("Revert the last upgrade?", dialogs.Messages.Single(), StringComparison.Ordinal);
        Assert.Equal("Before", File.ReadAllText(script));
        Assert.Equal(before, File.ReadAllText(project));
        Assert.False(fixture.Session.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task RemoveDeletesTheReportTheBackupAndItsRecord()
    {
        using var fixture = ShellSessionFixture.Create();
        var project = Directory.GetFiles(fixture.PackageDirectory, "*.csproj").Single();
        var backup = new UpgradeBackup(fixture.PackageDirectory, DateTime.Now);
        backup.Snapshot(project);
        VSProjectHelper.SetUpgradeRecord(project, "Stride", "4.5.5", backupFolder: backup.FolderPath);
        var report = Path.Combine(fixture.PackageDirectory, UpgradeReportWriter.FileName);
        File.WriteAllText(report, "# Upgrade report: Game.csproj\n");
        await fixture.OpenAsync();
        var dialogs = fixture.ReplaceDialogService();
        var banner = fixture.Main.UpgradeReportBanner;
        Assert.True(await WaitAsync(() => banner.CanRemoveReports && banner.CanRevert));

        banner.RemoveReportsCommand.Execute();

        Assert.True(await WaitAsync(() => !banner.IsVisible));
        Assert.Contains("the last upgrade can then no longer be reverted", dialogs.Messages.Single(), StringComparison.Ordinal);
        Assert.False(File.Exists(report));
        Assert.False(Directory.Exists(backup.FolderPath));
        Assert.Empty(VSProjectHelper.GetUpgradeRecords(project).Backups);
    }

    // Records in the project of the fixture an upgrade kept unfinished, with these steps left
    private static string WriteUnfinishedRecord(ShellSessionFixture fixture, string steps)
    {
        var project = Directory.GetFiles(fixture.PackageDirectory, "*.csproj").Single();
        var document = XDocument.Load(project);
        document.Root!.Add(new XElement("ItemGroup", new XElement(PackageUpgrader.UpgradeRecordItem,
            new XAttribute("Include", "Stride"), new XAttribute("Version", "4.5.5"), new XAttribute(PackageUpgrader.UnfinishedMetadata, steps))));
        document.Save(project);
        return project;
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
