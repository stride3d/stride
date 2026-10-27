// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Scripts;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="ScriptSourceFileAsset"/>.
/// </summary>
/// <remarks>
/// The text of a script is edited by its editor, outside the action history: the editor reports its unsaved edits with
/// <see cref="SetHasUnsavedText"/>, and the script is dirty while it has some, as in the WPF editor. The editor writes the file
/// itself, so the session does not write the script for these edits (its asset item is dirty only for the changes of the
/// action history, such as a rename).
/// </remarks>
[AssetViewModel<ScriptSourceFileAsset>]
public class ScriptSourceFileViewModel : SourceCodeViewModel<ScriptSourceFileAsset>
{
    private bool hasUnsavedText;
    private bool isHistoryDirty;

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
