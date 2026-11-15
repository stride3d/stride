// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

public sealed class TestSessionContract
{
    [Fact]
    public void ThePluginFacingSessionDoesNotExposeTheSessionPlumbing()
    {
        // The session objects keep the session up to date through these members: a plugin must not call them
        string[] plumbing =
        [
            nameof(ISessionInternals.IsInFixupAssetContext),
            nameof(ISessionInternals.UpdateHasUnsavedChanges),
            nameof(ISessionInternals.CheckConsistency),
            nameof(ISessionInternals.CheckAssetReplacements),
            nameof(ISessionInternals.RefreshRootAssetSelection),
            nameof(ISessionInternals.RegisterAsset),
            nameof(ISessionInternals.UnregisterAsset),
            nameof(ISessionInternals.SetFileDeletedOutside),
            nameof(ISessionInternals.LoadAddedDependencies),
        ];

        var members = typeof(ISessionViewModel).GetMembers().Select(x => x.Name).ToHashSet();
        Assert.Empty(plumbing.Where(members.Contains));
        Assert.False(typeof(ISessionInternals).IsPublic);
        Assert.True(typeof(ISessionInternals).IsAssignableFrom(typeof(SessionViewModel)));
    }
}
