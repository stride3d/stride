// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The project the session makes current when it opens a game: the one Build project and Start project use.
/// </summary>
public sealed class CurrentProjectTests
{
    [Fact]
    public async Task TheExecutableOfTheEditorPlatformIsTheCurrentProject()
    {
        using var fixture = await EditorSessionFixture.OpenGameAsync(PlatformType.Windows, PlatformType.Linux, PlatformType.macOS);

        var current = Assert.IsType<ProjectViewModel>(fixture.Session.CurrentProject);
        Assert.Equal(ProjectType.Executable, current.Type);
        Assert.Equal(Platform.Type, current.Platform);
        Assert.Equal($"{EditorSessionFixture.PackageName}.{Platform.Type}", current.Name);
    }

    [SkippableFact]
    public async Task WithoutAnExecutableOfTheEditorPlatformTheWindowsOneIsTheCurrentProject()
    {
        Skip.If(Platform.Type == PlatformType.Windows, "The Windows executable is the one of the editor's platform.");
        using var fixture = await EditorSessionFixture.OpenGameAsync(PlatformType.Android, PlatformType.Windows);

        var current = Assert.IsType<ProjectViewModel>(fixture.Session.CurrentProject);
        Assert.Equal(PlatformType.Windows, current.Platform);
    }
}
