// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The session of the test kit of editor plugins (Stride.Core.Assets.Editor.Testing), with any plugin host: it writes the package,
/// opens it in the dispatcher and cleans everything up.
/// </summary>
public sealed class EditorTestSessionTests
{
    [Fact]
    public async Task TheSessionOpensThePackageInTheDispatcherAndDisposingRemovesIt()
    {
        string directory;
        await using (var fixture = await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService(), PackageName = "MyGame" },
            ("Folder/Value", new TestEditorAsset { Value = "A" })))
        {
            directory = fixture.Directory;
            // The test goes on in the dispatcher, where it can change the session
            fixture.Session.Dispatcher.EnsureAccess();
            Assert.Equal("MyGame", fixture.Session.LocalPackages.Single().Name);
            var asset = fixture.Asset<AssetViewModel>("Folder/Value");
            Assert.Equal("/MyGame/Folder/Value", asset.Url);
            Assert.Equal("A", ((TestEditorAsset)asset.Asset).Value);
            Assert.Throws<InvalidOperationException>(() => fixture.Asset<AssetViewModel>("Missing"));
        }
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task SessionOpenedRunsInTheDispatcherWithTheOpenSession()
    {
        var opened = false;
        await using var fixture = await EditorTestSession.OpenAsync(new EditorTestSessionOptions
        {
            Plugins = new FakePluginService(),
            SessionOpened = session =>
            {
                session.Dispatcher.EnsureAccess();
                opened = true;
            },
        });
        Assert.True(opened);
    }

    [Fact]
    public async Task ASessionThatAnotherTestOpensWhileOneIsOpenFailsAtOnceWithTheCause()
    {
        await using var fixture = await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService() });

        // Another test, run in parallel with this one: a flow of its own, out of the dispatcher
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => InAnotherTest(async () =>
        {
            await using var other = await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService() });
        }));
        Assert.Contains("Another test has a session of the editor test kit open", error.Message);
        Assert.Contains("[assembly: CollectionBehavior(DisableTestParallelization = true)]", error.Message);

        // The failed open left nothing behind: this test still opens a second session
        await using var second = await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService() });
        Assert.NotSame(fixture.Session, second.Session);
    }

    [Fact]
    public async Task SessionsOpenedOneAfterTheOtherOrTogetherByOneTestAreNotCaught()
    {
        // One after the other, by this test and by another one that runs after it
        await using (await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService() }))
        {
        }
        await InAnotherTest(async () =>
        {
            await using var other = await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService() });
            Assert.NotNull(other.Session);
        });

        // Two at once, opened directly or by a helper of the test
        await using var first = await OpenWithHelperAsync();
        await using var second = await OpenWithHelperAsync();
        await using var third = await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService() });
        Assert.Equal(3, new[] { first.Session, second.Session, third.Session }.Distinct().Count());
    }

    private static async DispatcherTask<EditorTestSession> OpenWithHelperAsync()
        => await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService() });

    /// <summary>
    /// Runs the test code as another test would: on the thread pool, out of the dispatcher, without the flow of this test.
    /// </summary>
    private static Task InAnotherTest(Func<Task> test)
    {
        using (ExecutionContext.SuppressFlow())
            return Task.Run(test);
    }
}
