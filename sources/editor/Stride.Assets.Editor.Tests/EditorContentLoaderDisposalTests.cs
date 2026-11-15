// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame.ContentLoader;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The session raises the events the content loader follows from the thread pool, so a handler raised just before the editor
/// closes still runs after the loader is disposed. It must stop quietly: an exception of an <c>async void</c> handler ends the
/// process (the test host crash of stack 4's first CI run).
/// </summary>
public sealed class EditorContentLoaderDisposalTests
{
    [Fact]
    public async Task AHandlerThatFindsTheDatabaseDisposedStopsQuietly()
    {
        var disposed = false;
        await EditorContentLoader.RunUnlessDisposedAsync(async () =>
        {
            await Task.Yield();
            // The editor closes while the handler waits: the database it locks next is gone
            disposed = true;
            throw new ObjectDisposedException("GameStudioDatabase");
        }, () => disposed);
    }

    [Fact]
    public async Task AHandlerRaisedAfterTheLoaderIsDisposedDoesNothing()
    {
        var ran = false;
        await EditorContentLoader.RunUnlessDisposedAsync(() =>
        {
            ran = true;
            return Task.CompletedTask;
        }, () => true);
        Assert.False(ran);
    }

    [Fact]
    public async Task AnObjectDisposedExceptionOfAnOpenLoaderIsStillAnError()
    {
        // Only the closing of the editor explains a disposed object: before that, it's a bug to report
        await Assert.ThrowsAsync<ObjectDisposedException>(() => EditorContentLoader.RunUnlessDisposedAsync(async () =>
        {
            await Task.Yield();
            throw new ObjectDisposedException("GameStudioDatabase");
        }, () => false));
    }
}
