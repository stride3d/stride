// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Tests.Helpers;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// With a session open, File > New, Open, Open recent and Reload start a new editor process, as WPF's restart does: a second
/// session would load the package assemblies again in this process. The save question comes first, and Cancel keeps the session.
/// </summary>
public sealed class ProcessRestartTests
{
    private const string AssetLocation = "Text";

    /// <summary>
    /// The command line of this editor: the options a new one gets again.
    /// </summary>
    private static readonly string[] CommandLine = ["/DisablePreview", "--graphics-api", "Vulkan"];

    public static TheoryData<string> Commands => ["New", "Open", "Open recent", "Reload"];

    [AvaloniaTheory]
    [MemberData(nameof(Commands))]
    public async Task WithASessionOpenTheCommandStartsANewProcess(string command)
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var dialogs = fixture.ReplaceDialogService();
        var launcher = UseProcessLauncher(fixture);

        await ExecuteAsync(fixture, command);
        await WindowTestHelper.WaitUntilAsync(() => launcher.Starts.Count > 0, "the start of the new editor");

        // The same game again for Open, Open recent and Reload; the project selection for New
        var session = command == "New" ? "/NewProject" : fixture.PackagePath.ToOSPath();
        Assert.Equal([.. CommandLine, session], Assert.Single(launcher.Starts));
        // No change: no question
        Assert.Empty(dialogs.Messages);
        Assert.Null(fixture.Main.Session);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Commands))]
    public async Task TheSaveQuestionComesFirst(string command)
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var dialogs = fixture.ReplaceDialogService();
        var launcher = UseProcessLauncher(fixture);
        // What was saved when the new editor started
        string? savedAtStart = null;
        launcher.Started = () => savedAtStart = ReadLabel(fixture);
        Edit(fixture, "edited");
        dialogs.PickButton = x => x.Single(b => (string)b.Content! == "Save").Result;

        await ExecuteAsync(fixture, command);
        await WindowTestHelper.WaitUntilAsync(() => launcher.Starts.Count > 0, "the start of the new editor");

        Assert.Equal("The project has unsaved changes. Do you want to save it?", Assert.Single(dialogs.Messages));
        Assert.Equal("edited", savedAtStart);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Commands))]
    public async Task CancelKeepsTheSessionOpen(string command)
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var dialogs = fixture.ReplaceDialogService();
        var launcher = UseProcessLauncher(fixture);
        var session = fixture.Session;
        Edit(fixture, "edited");
        dialogs.PickButton = x => x.Single(b => b.IsCancel).Result;

        await ExecuteAsync(fixture, command);
        await WindowTestHelper.PumpAsync();

        Assert.Single(dialogs.Messages);
        Assert.Empty(launcher.Starts);
        Assert.Same(session, fixture.Main.Session);
        Assert.True(session.HasUnsavedAssets());
        Assert.Equal("label", ReadLabel(fixture));
    }

    [AvaloniaFact]
    public async Task AfterClosingTheSessionTheNextOneOpensInANewProcessToo()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        fixture.ReplaceDialogService();
        var launcher = UseProcessLauncher(fixture);
        // The session was loaded in this process
        launcher.Service.NotifySessionLoading();

        // File > Close empties the editor in this process; the package assemblies stay loaded
        fixture.Main.CloseCommand.Execute();
        await WindowTestHelper.WaitUntilAsync(() => fixture.Main.Session is null, "the close of the session");
        Assert.Empty(launcher.Starts);

        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        fixture.Main.NewSessionCommand.Execute();
        await WindowTestHelper.WaitUntilAsync(() => launcher.Starts.Count == 2, "the start of the new editors");

        Assert.Equal([.. CommandLine, fixture.PackagePath.ToOSPath()], launcher.Starts[0]);
        Assert.Equal([.. CommandLine, "/NewProject"], launcher.Starts[1]);
        Assert.Null(fixture.Main.Session);
    }

    [AvaloniaFact]
    public async Task TheFirstSessionOpensInThisProcess()
    {
        using var fixture = ShellSessionFixture.Create((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var launcher = UseProcessLauncher(fixture);

        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));

        Assert.Empty(launcher.Starts);
        Assert.NotNull(fixture.Main.Session);
        Assert.True(launcher.Service.HasLoadedSession);
    }

    private static Task ExecuteAsync(ShellSessionFixture fixture, string command)
    {
        switch (command)
        {
            case "New":
                fixture.Main.NewSessionCommand.Execute();
                return Task.CompletedTask;
            case "Open":
                return fixture.Main.OpenSession(fixture.PackagePath);
            case "Open recent":
                fixture.Main.RecentFiles.Single(x => x.FilePath == fixture.PackagePath).OpenCommand.Execute();
                return Task.CompletedTask;
            case "Reload":
                fixture.Main.ReloadCommand.Execute();
                return Task.CompletedTask;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }

    /// <summary>
    /// Gives the editor a process service that records the processes it starts instead of starting them.
    /// </summary>
    private static RecordingProcessLauncher UseProcessLauncher(ShellSessionFixture fixture)
    {
        var launcher = new RecordingProcessLauncher();
        launcher.Service = new EditorProcessService(launcher, StartupOptions.Parse(CommandLine));
        fixture.ServiceProvider.RegisterService(launcher.Service);
        return launcher;
    }

    /// <summary>
    /// Sets the label of the asset the way the property grid does: through its property graph, in a transaction of the history.
    /// </summary>
    private static void Edit(ShellSessionFixture fixture, string label)
    {
        var undoRedo = fixture.Session.UndoRedoService!;
        using var transaction = undoRedo.CreateTransaction();
        fixture.Asset(AssetLocation).PropertyGraph.RootNode[nameof(TextPropertiesAsset.Label)].Update(label);
        undoRedo.SetName(transaction, "Edit label");
    }

    /// <summary>
    /// The label saved in the file of the asset.
    /// </summary>
    private static string? ReadLabel(ShellSessionFixture fixture)
    {
        var path = Path.Combine(fixture.PackageDirectory, "Assets", AssetLocation + ".sdtextpropertiestest");
        return ((TextPropertiesAsset)AssetFileSerializer.Load<TextPropertiesAsset>(path).Asset).Label;
    }
}
