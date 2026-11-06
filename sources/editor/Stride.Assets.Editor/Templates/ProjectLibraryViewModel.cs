// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The name (and namespace) asked when a code library or a plugin project is added to the session, and the project that references
/// it when several could. <see cref="HasError"/> follows the name as the user types it; <see cref="Validate"/> checks the names
/// when the user accepts.
/// </summary>
public sealed class ProjectLibraryViewModel : ViewModelBase
{
    private readonly Func<string, bool> isNameTaken;
    private string libraryName;
    private string ns;
    private bool hasError;
    private string? selectedReferencingProject;

    /// <param name="defaultName">The initial name, also the initial namespace.</param>
    /// <param name="isNameTaken">Whether a name collides with a project of the session.</param>
    /// <param name="title">The window title; the code library one when null.</param>
    /// <param name="referencingProjects">The projects offered as the one to reference the new project from; none hides the choice.</param>
    /// <param name="showNamespace">Whether the namespace is asked for; otherwise it is the library name.</param>
    /// <param name="nameLabel">The label of the name box; "Library name:" when null.</param>
    public ProjectLibraryViewModel(string defaultName, Func<string, bool> isNameTaken, string? title = null, IReadOnlyList<string>? referencingProjects = null, bool showNamespace = true, string? nameLabel = null)
    {
        ArgumentNullException.ThrowIfNull(defaultName);
        ArgumentNullException.ThrowIfNull(isNameTaken);
        this.isNameTaken = isNameTaken;
        libraryName = defaultName;
        ns = defaultName;
        Title = title ?? Tr._p("Title", "New code library");
        NameLabel = nameLabel ?? Tr._p("Label", "Library name:");
        ShowNamespace = showNamespace;
        ReferencingProjects = referencingProjects ?? [];
        selectedReferencingProject = ReferencingProjects.Count > 0 ? ReferencingProjects[0] : null;
        hasError = isNameTaken(defaultName);
    }

    public string Title { get; }

    public string NameLabel { get; }

    public bool ShowNamespace { get; }

    public IReadOnlyList<string> ReferencingProjects { get; }

    public bool HasReferencingProjects => ReferencingProjects.Count > 0;

    /// <summary>
    /// The name of the new project; <see cref="HasError"/> tells whether it is taken.
    /// </summary>
    public string LibraryName
    {
        get => libraryName;
        set
        {
            if (SetValue(ref libraryName, value))
                HasError = isNameTaken(value);
        }
    }

    public string Namespace
    {
        get => ns;
        set => SetValue(ref ns, value);
    }

    /// <summary>
    /// Whether <see cref="LibraryName"/> is the name of a project of the session already.
    /// </summary>
    public bool HasError
    {
        get => hasError;
        private set => SetValue(ref hasError, value);
    }

    public string? SelectedReferencingProject
    {
        get => selectedReferencingProject;
        set => SetValue(ref selectedReferencingProject, value);
    }

    /// <summary>
    /// Checks that the name (and the namespace, when asked for) are valid namespaces.
    /// </summary>
    /// <param name="error">The message to show when the result is false.</param>
    public bool Validate(out string? error)
    {
        if (HasError)
        {
            error = Tr._p("Message", "A project with this name exists already.");
            return false;
        }
        if (!NamingHelper.IsValidNamespace(LibraryName, out var nameError))
        {
            error = string.Format(Tr._p("Message", "Type a valid library name. Error with {0}"), nameError);
            return false;
        }
        if (ShowNamespace && !NamingHelper.IsValidNamespace(Namespace, out var namespaceError))
        {
            error = string.Format(Tr._p("Message", "Type a valid namespace name. Error with {0}"), namespaceError);
            return false;
        }
        error = null;
        return true;
    }
}
