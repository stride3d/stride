// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The reload of the game code when it changes, with a fake watcher, a fake builder and a fake restart: the question, the settings
/// of the WPF editor, and the builds it waits for.
/// </summary>
public sealed class GameCodeReloadViewModelTests
{
    private const string Question = "Some game code files have been modified. Do you want to reload the assemblies?";
    private const int ReloadButton = 1;
    private const int DontReloadButton = 2;

    [Fact]
    public async Task AChangeAsksOnceThenReloadBuildsTheChangedProjectAndRestarts()
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => ReloadButton;
        Assert.False(test.Reload.ReloadCommand.IsEnabled);

        test.Change("Player.cs", "Enemy.cs");
        await test.Reload.Question;

        Assert.Single(test.Dialogs.Messages, x => x.StartsWith(Question, StringComparison.Ordinal));
        var request = Assert.Single(test.Builder.Requests);
        Assert.Equal(test.ProjectPath, request.ProjectPath);
        Assert.Equal("AnyCPU", request.Platform);
        Assert.Equal(1, test.Restarts);
    }

    [Fact]
    public async Task DontReloadKeepsTheChangesPendingAndTheNextChangesDoNotAskAgain()
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => DontReloadButton;

        test.Change("Player.cs");
        await test.Reload.Question;
        Assert.True(test.Reload.HasPendingChanges);
        Assert.True(test.Reload.ReloadCommand.IsEnabled);
        Assert.Equal([test.ProjectPath], test.Reload.ChangedProjects);

        // As in WPF, the changes are reloaded later with the command
        test.Change("Enemy.cs");
        await test.Reload.Question;
        Assert.Single(test.Dialogs.Messages);
        Assert.Empty(test.Builder.Requests);
        Assert.Equal(0, test.Restarts);

        test.Reload.ReloadCommand.Execute();
        await WaitForAsync(() => test.Restarts == 1);
        Assert.Single(test.Builder.Requests);
        Assert.Single(test.Dialogs.Messages);
    }

    [Fact]
    public async Task ClosingTheQuestionWithoutAnAnswerKeepsTheChangesPending()
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => 0;

        test.Change("Player.cs");
        await test.Reload.Question;

        Assert.True(test.Reload.ReloadCommand.IsEnabled);
        Assert.Empty(test.Builder.Requests);
        Assert.Equal(0, test.Restarts);
    }

    [Fact]
    public async Task KeepingTheSessionInTheSaveQuestionKeepsTheChangesPending()
    {
        await using var test = await ReloadTest.CreateAsync(restarts: false);
        test.Dialogs.PickButton = _ => ReloadButton;

        test.Change("Player.cs");
        await test.Reload.Question;

        Assert.Equal(1, test.Restarts);
        Assert.True(test.Reload.HasPendingChanges);
        Assert.True(test.Reload.ReloadCommand.IsEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DontAskAgainStoresTheAnswer(bool reload)
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => reload ? ReloadButton : DontReloadButton;
        test.Dialogs.CheckBoxAnswer = true;

        test.Change("Player.cs");
        await test.Reload.Question;

        Assert.False(EditorSettings.AskBeforeReloadingAssemblies.GetValue(test.Settings, true));
        Assert.Equal(reload, EditorSettings.AutoReloadAssemblies.GetValue(test.Settings, true));
        Assert.Equal(1, test.SettingsSaved);
        Assert.Equal(reload ? 1 : 0, test.Restarts);
    }

    [Fact]
    public async Task WithoutAskingTheSettingReloadsOrNot()
    {
        await using var test = await ReloadTest.CreateAsync();
        EditorSettings.AskBeforeReloadingAssemblies.SetValue(false, test.Settings);
        EditorSettings.AutoReloadAssemblies.SetValue(true, test.Settings);

        test.Change("Player.cs");
        await test.Reload.Question;
        Assert.Empty(test.Dialogs.Messages);
        Assert.Equal(1, test.Restarts);

        // Never: the changes wait for the command
        await using var never = await ReloadTest.CreateAsync();
        EditorSettings.AskBeforeReloadingAssemblies.SetValue(false, never.Settings);
        EditorSettings.AutoReloadAssemblies.SetValue(false, never.Settings);

        never.Change("Player.cs");
        await never.Reload.Question;
        Assert.Empty(never.Dialogs.Messages);
        Assert.Empty(never.Builder.Requests);
        Assert.Equal(0, never.Restarts);
        Assert.True(never.Reload.ReloadCommand.IsEnabled);
    }

    [Fact]
    public async Task AChangeDuringABuildAsksWhenTheBuildEnds()
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => DontReloadButton;
        test.Builder.CompleteAtOnce = false;
        var started = test.Builder.NextBuild;
        // A build of the session: the one of F6, or the one of a reload
        var building = test.Build.BuildCodeAsync([test.ProjectPath]);
        var build = await started.WaitAsync(TimeSpan.FromSeconds(30));

        test.Change("Player.cs");
        await test.Reload.Question;
        Assert.Empty(test.Dialogs.Messages);
        // The reload waits for the build too
        Assert.False(test.Reload.ReloadCommand.IsEnabled);

        build.Complete(true);
        await building;
        await test.Reload.Question;
        Assert.Single(test.Dialogs.Messages);
        Assert.True(test.Reload.ReloadCommand.IsEnabled);
    }

    [Fact]
    public async Task TheQuestionWaitsForTheMainWindow()
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => DontReloadButton;
        var mainWindowActive = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        test.Dialogs.MainWindowActive = mainWindowActive.Task;

        test.Change("Player.cs");
        Assert.False(test.Reload.Question.IsCompleted);
        Assert.Empty(test.Dialogs.Messages);

        mainWindowActive.SetResult();
        await test.Reload.Question;
        Assert.Single(test.Dialogs.Messages);
    }

    [Fact]
    public async Task AFailedBuildKeepsTheSessionAndTheNextChangeAsksAgain()
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => ReloadButton;
        test.Builder.Succeed = false;

        test.Change("Player.cs");
        await test.Reload.Question;
        Assert.Equal(0, test.Restarts);
        Assert.True(test.Reload.ReloadCommand.IsEnabled);
        test.Build.BuildLog.Flush();
        Assert.Contains($"Build of {EditorSessionFixture.PackageName} failed.", test.Build.BuildLog.Messages.Select(x => x.Text));

        // The fix
        test.Builder.Succeed = true;
        test.Change("Player.cs");
        await test.Reload.Question;
        Assert.Equal(2, test.Dialogs.Messages.Count);
        Assert.Equal(1, test.Restarts);
    }

    [Fact]
    public async Task ACancelledBuildKeepsTheChangesPending()
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => ReloadButton;
        test.Builder.CompleteAtOnce = false;
        var started = test.Builder.NextBuild;

        test.Change("Player.cs");
        await started.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(test.Build.CancelBuildCommand.IsEnabled);
        test.Build.CancelBuildCommand.Execute();
        await test.Reload.Question;

        Assert.Equal(0, test.Restarts);
        Assert.True(test.Reload.ReloadCommand.IsEnabled);
        // Cancelled is not failed: the next change does not ask
        test.Change("Player.cs");
        await test.Reload.Question;
        Assert.Single(test.Dialogs.Messages);
    }

    [Fact]
    public async Task AnotherSessionForgetsTheChanges()
    {
        await using var test = await ReloadTest.CreateAsync();
        test.Dialogs.PickButton = _ => DontReloadButton;
        test.Change("Player.cs");
        await test.Reload.Question;
        Assert.True(test.Reload.HasPendingChanges);

        test.Reload.Session = null;
        Assert.False(test.Reload.HasPendingChanges);
        Assert.False(test.Reload.ReloadCommand.IsEnabled);
        Assert.Empty(test.Reload.ChangedProjects);
        Assert.Single(test.Watchers, x => x.IsDisposed);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!condition())
        {
            if (DateTime.UtcNow > timeout)
                throw new TimeoutException();
            await Task.Delay(10);
        }
    }

    /// <summary>
    /// A session, its builds with a fake builder, and the reload of its code with a watcher that does not watch the file system and
    /// a restart that counts its calls.
    /// </summary>
    private sealed class ReloadTest : IAsyncDisposable
    {
        private ReloadTest(EditorSessionFixture fixture, bool restarts)
        {
            Fixture = fixture;
            Builder = new FakeGameProjectBuilder();
            fixture.ServiceProvider.RegisterService(Builder);
            fixture.ServiceProvider.RegisterService(new FakeGameLauncher());
            Settings = EditorSettings.SettingsContainer.CreateSettingsProfile(false, registerInContainer: false);
            ProjectPath = Path.GetFullPath(((ProjectViewModel)fixture.Package).ProjectPath.ToOSPath());
            Build = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
            Reload = new GameCodeReloadViewModel(fixture.ServiceProvider, Build, () =>
            {
                ++Restarts;
                return Task.FromResult(restarts);
            })
            {
                CreateWatcher = _ =>
                {
                    var watcher = new GameCodeWatcher([ProjectPath], watchFileSystem: false);
                    Watchers.Add(watcher);
                    return watcher;
                },
                SettingsProfile = Settings,
                SaveSettings = () => ++SettingsSaved,
            };
            Reload.Session = fixture.Session;
        }

        public EditorSessionFixture Fixture { get; }

        public FakeEditorDialogService Dialogs => Fixture.Dialogs;

        public FakeGameProjectBuilder Builder { get; }

        public BuildAndRunViewModel Build { get; }

        public GameCodeReloadViewModel Reload { get; }

        public SettingsProfile Settings { get; }

        public string ProjectPath { get; }

        public List<GameCodeWatcher> Watchers { get; } = [];

        public int Restarts { get; private set; }

        public int SettingsSaved { get; private set; }

        public static async Task<ReloadTest> CreateAsync(bool restarts = true)
        {
            return new ReloadTest(await EditorSessionFixture.OpenAsync(), restarts);
        }

        /// <summary>
        /// A burst of changes of source files of the project.
        /// </summary>
        public void Change(params string[] files)
        {
            var directory = Path.GetDirectoryName(ProjectPath)!;
            Reload.OnCodeChanged(files.Select(x => new GameCodeChange(ProjectPath, Path.Combine(directory, x))).ToList());
        }

        public ValueTask DisposeAsync()
        {
            Reload.Destroy();
            Build.Destroy();
            Fixture.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
