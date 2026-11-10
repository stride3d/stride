// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The reload of the game code from the shell: a change of a source file of a project the editor loads asks WPF's question, and
/// Reload builds the project and opens the session in a new editor (Reload project). The builder and the new editor are fakes.
/// </summary>
public sealed class GameCodeReloadTests
{
    private const string Question = "Some game code files have been modified. Do you want to reload the assemblies?";

    [AvaloniaFact]
    public async Task TheProjectMenuAndTheToolbarReloadTheGameCodeOnceItChanged()
    {
        using var fixture = await ShellSessionFixture.OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var menuItem = window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == "ReloadGameCodeMenuItem");
            var button = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "ReloadGameCodeButton");
            Assert.Same(fixture.Main.CodeReload.ReloadCommand, menuItem.Command);
            Assert.Same(fixture.Main.CodeReload.ReloadCommand, button.Command);
            Assert.Same(fixture.Session, fixture.Main.CodeReload.Session);
            // Nothing changed
            Assert.False(button.IsEffectivelyEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ChangingASourceFileAsksThenReloadBuildsAndOpensTheSessionInANewEditor()
    {
        using var fixture = await ShellSessionFixture.OpenAsync();
        var dialogs = fixture.ReplaceDialogService();
        var (builder, _) = fixture.ReplaceGameBuilder();
        var launcher = new RecordingProcessLauncher();
        launcher.Service = new EditorProcessService(launcher, StartupOptions.Parse([]));
        fixture.ServiceProvider.RegisterService(launcher.Service);
        launcher.Service.NotifySessionLoading();
        // Reload, the first button
        dialogs.PickButton = buttons => buttons.First().Result;

        // The project of the fixture holds no asset types, so the session did not load its assembly: as if it had, it is watched
        var projectPath = Path.Combine(fixture.PackageDirectory, ShellSessionFixture.PackageName + ".csproj");
        var session = fixture.Session;
        fixture.Package.Package.LoadedAssemblies.Add(new PackageLoadedAssembly(new ProjectReference(Guid.NewGuid(), new UFile(projectPath), ProjectType.Library), Path.Combine(fixture.Directory, "bin", "TestGame.dll")));
        fixture.Main.CodeReload.Session = null;
        fixture.Main.CodeReload.Session = session;

        // The build output does not count
        Directory.CreateDirectory(Path.Combine(fixture.PackageDirectory, "obj"));
        await File.WriteAllTextAsync(Path.Combine(fixture.PackageDirectory, "obj", "Generated.cs"), "// generated");
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        await WindowTestHelper.PumpAsync();
        Assert.Empty(dialogs.Messages);
        Assert.False(fixture.Main.CodeReload.HasPendingChanges);

        await File.WriteAllTextAsync(Path.Combine(fixture.PackageDirectory, "Player.cs"), "public class Player { }");
        await WindowTestHelper.WaitUntilAsync(() => launcher.Starts.Count == 1, "the start of the new editor", 30000);

        Assert.StartsWith(Question, Assert.Single(dialogs.Messages));
        Assert.Equal(projectPath, Assert.Single(builder.Requests).ProjectPath);
        Assert.Equal(fixture.PackagePath.ToOSPath(), Assert.Single(launcher.Starts).Last());
        Assert.Null(fixture.Main.Session);
        Assert.Null(fixture.Main.CodeReload.Session);
    }
}
