// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Scripts;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="ScriptSourceFileAsset"/>.
/// </summary>
/// <remarks>
/// The text of a script is edited by its editor, outside the action history: the editor reports its unsaved edits with
/// <see cref="SetHasUnsavedText"/>, and the script is dirty while it has some, as in the WPF editor. The editor writes the file
/// itself, so the session does not write the script for these edits (its asset item is dirty only for the changes of the
/// action history, such as a rename).
/// <para>
/// The script also keeps the signature of the file its text was read from. Before the session writes the scripts (a rename
/// writes the text to the new file), a script whose file another program changed takes the text of the file, unless its
/// editor has unsaved edits: so the change is kept whether the editor of the script is open, closed or was never opened.
/// </para>
/// </remarks>
[AssetViewModel<ScriptSourceFileAsset>]
public class ScriptSourceFileViewModel : SourceCodeViewModel<ScriptSourceFileAsset>
{
    private bool hasUnsavedText;
    private bool isHistoryDirty;
    // The file that the text was read from or written to, and its signature then; null when the script has no file to follow
    private string? filePath;
    private FileContentSignature signature;

    public ScriptSourceFileViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    /// Gets whether the editor of the script has edits of its text that are not saved to the file.
    /// </summary>
    public bool HasUnsavedText => hasUnsavedText;

    /// <summary>
    /// Records whether the editor of the script has edits of its text that are not saved to the file.
    /// </summary>
    /// <param name="value"><c>true</c> when the text has unsaved edits.</param>
    public void SetHasUnsavedText(bool value)
    {
        if (hasUnsavedText == value)
            return;

        hasUnsavedText = value;
        OnPropertyChanged(nameof(HasUnsavedText));
        base.UpdateDirtiness(isHistoryDirty || hasUnsavedText);
    }

    /// <inheritdoc />
    protected internal override void Initialize()
    {
        // The base class reads the text of a project file
        base.Initialize();
        if (!Directory.Package.Package.IsReadOnly)
            CaptureFile();
    }

    /// <inheritdoc />
    protected internal override void OnSessionSaving()
    {
        base.OnSessionSaving();
        if (filePath == null || hasUnsavedText)
            return;

        // The file is read where the text came from: until the session saves a rename, the script keeps its old file
        var current = FileContentSignature.Capture(filePath);
        if (current == signature || !current.Exists)
            return;

        try
        {
            var text = File.ReadAllText(filePath);
            if (Asset.Text != text)
                Asset.Text = text;
            signature = current;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Another program still holds the file: the session writes the text it has, as before
        }
    }

    /// <inheritdoc />
    protected internal override void OnSessionSaved()
    {
        base.OnSessionSaved();
        // The session wrote the script (or had nothing to write): its file is the one of its current name. With unsaved
        // edits, the editor has not written the file, so the old signature stays and the next save still checks it.
        if (filePath != null && !hasUnsavedText)
            CaptureFile();
    }

    private void CaptureFile()
    {
        filePath = FullPath?.ToOSPath();
        signature = FileContentSignature.Capture(filePath);
    }

    /// <inheritdoc />
    protected override void UpdateDirtiness(bool value)
    {
        // The dirtiness of the action history, combined with the unsaved edits of the editor
        isHistoryDirty = value;
        base.UpdateDirtiness(value || hasUnsavedText);
    }

    /// <inheritdoc />
    protected override void OnDirtyFlagSet()
    {
        // Only the changes of the action history make the session write the script: its editor writes the text itself
        AssetItem.IsDirty = isHistoryDirty;
    }
}
