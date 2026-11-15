// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Templates;

namespace Stride.Assets.Editor.Tests.Templates;

/// <summary>
/// The view model of the name prompt shown when a code library or a plugin is added to the session.
/// </summary>
public sealed class ProjectLibraryViewModelTests
{
    private static readonly string[] ExistingProjects = ["MyGame.Game", "MyGame.Windows"];

    [Fact]
    public void TheDefaultsAreTheLibraryOnes()
    {
        var library = new ProjectLibraryViewModel("MyLibrary", ExistingProjects.Contains);

        Assert.Equal("New code library", library.Title);
        Assert.Equal("Library name:", library.NameLabel);
        Assert.Equal("MyLibrary", library.LibraryName);
        Assert.Equal("MyLibrary", library.Namespace);
        Assert.True(library.ShowNamespace);
        Assert.False(library.HasError);
        Assert.False(library.HasReferencingProjects);
        Assert.Null(library.SelectedReferencingProject);
        Assert.True(library.Validate(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void ThePluginPromptHasItsOwnTitleAndNoNamespace()
    {
        var plugin = new ProjectLibraryViewModel("MyPlugin", ExistingProjects.Contains, "New plugin", showNamespace: false, nameLabel: "Plugin name:");

        Assert.Equal("New plugin", plugin.Title);
        Assert.Equal("Plugin name:", plugin.NameLabel);
        Assert.False(plugin.ShowNamespace);
        // The namespace is not asked, so it is not checked either
        plugin.Namespace = "not a namespace";
        Assert.True(plugin.Validate(out _));
    }

    [Fact]
    public void ATakenNameIsAnError()
    {
        var library = new ProjectLibraryViewModel("MyGame.Game", ExistingProjects.Contains);
        Assert.True(library.HasError);
        Assert.False(library.Validate(out var error));
        Assert.Equal("A project with this name exists already.", error);

        library.LibraryName = "MyGame.Core";
        Assert.False(library.HasError);
        Assert.True(library.Validate(out _));

        library.LibraryName = "MyGame.Windows";
        Assert.True(library.HasError);
    }

    [Fact]
    public void TheNamesMustBeValidNamespaces()
    {
        var library = new ProjectLibraryViewModel("MyLibrary", ExistingProjects.Contains);

        library.LibraryName = "1Library";
        Assert.False(library.HasError);
        Assert.False(library.Validate(out var error));
        Assert.StartsWith("Type a valid library name.", error);

        library.LibraryName = "MyLibrary";
        library.Namespace = "My Namespace";
        Assert.False(library.Validate(out error));
        Assert.StartsWith("Type a valid namespace name.", error);

        library.Namespace = "My.Namespace";
        Assert.True(library.Validate(out error));
        Assert.Null(error);
    }

    [Fact]
    public void TheFirstReferencingProjectIsSelectedByDefault()
    {
        var library = new ProjectLibraryViewModel("MyLibrary", ExistingProjects.Contains, referencingProjects: ExistingProjects);

        Assert.True(library.HasReferencingProjects);
        Assert.Equal("MyGame.Game", library.SelectedReferencingProject);
        library.SelectedReferencingProject = "MyGame.Windows";
        Assert.Equal("MyGame.Windows", library.SelectedReferencingProject);
    }
}
