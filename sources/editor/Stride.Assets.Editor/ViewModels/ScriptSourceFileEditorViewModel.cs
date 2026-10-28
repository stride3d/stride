// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The editor of a script: the text of its file, which the editor saves itself (the editor has no command to save the session
/// yet), and reloads when another program changes the file while the editor has no unsaved edit.
/// </summary>
[AssetEditorViewModel<ScriptSourceFileViewModel>]
public sealed class ScriptSourceFileEditorViewModel : AssetEditorViewModel<ScriptSourceFileViewModel>
{
    /// <summary>
    /// The smallest font size of the editor.
    /// </summary>
    public const int MinimumFontSize = 8;

    /// <summary>
    /// The largest font size of the editor.
    /// </summary>
    public const int MaximumFontSize = 72;

    private string savedText;
    private string currentText;
    private bool isModified;
    private int fontSize = 13;
    private FileContentSignature signature;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptSourceFileEditorViewModel"/> class.
    /// </summary>
    /// <param name="asset">The script to edit.</param>
    public ScriptSourceFileEditorViewModel(ScriptSourceFileViewModel asset)
        : base(asset)
    {
        savedText = currentText = asset.Asset.Text ?? string.Empty;
        signature = FileContentSignature.Capture(FilePath);
        // Enabled while the text has unsaved edits
        SaveCommand = new AnonymousTaskCommand(ServiceProvider, SaveAsync) { IsEnabled = false };
    }

    /// <summary>
    /// Occurs when the editor reloaded the file after another program changed it, with the new text.
    /// </summary>
    public event EventHandler<string>? TextReloaded;

    /// <summary>
    /// Gets the current text of the script, with the unsaved edits.
    /// </summary>
    public string Text => currentText;

    /// <summary>
    /// Gets the command that saves the text to the file of the script.
    /// </summary>
    public ICommandBase SaveCommand { get; }

    /// <summary>
    /// Gets whether the text has edits that are not saved.
    /// </summary>
    public bool IsModified
    {
        get => isModified;
        private set => SetValue(ref isModified, value, () => SaveCommand.IsEnabled = value);
    }

    /// <summary>
    /// Gets or sets the font size of the editor, between <see cref="MinimumFontSize"/> and <see cref="MaximumFontSize"/>.
    /// </summary>
    public int FontSize
    {
        get => fontSize;
        set => SetValue(ref fontSize, Math.Clamp(value, MinimumFontSize, MaximumFontSize));
    }

    private string? FilePath => Asset.AssetItem.FullPath?.ToOSPath();

    /// <summary>
    /// Records an edit of the user.
    /// </summary>
    /// <param name="text">The text after the edit.</param>
    public void OnTextEdited(string text)
    {
        currentText = text ?? string.Empty;
        IsModified = !string.Equals(currentText, savedText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Reloads the file if another program changed it and the text has no unsaved edit; the view calls it when the editor gets
    /// the focus.
    /// </summary>
    public void CheckExternalChanges()
    {
        if (IsModified)
            return;

        var path = FilePath;
        var current = FileContentSignature.Capture(path);
        if (current == signature)
            return;

        savedText = currentText = current.Exists ? File.ReadAllText(path!) : string.Empty;
        signature = current;
        Asset.Asset.Text = currentText;
        TextReloaded?.Invoke(this, currentText);
    }

    /// <summary>
    /// Saves the text to the file of the script, after asking the user when another program changed the file since the editor
    /// read it. Without unsaved edits, it does nothing.
    /// </summary>
    /// <returns>A task that completes when the file is saved, or when the user chose not to overwrite it.</returns>
    public async Task SaveAsync()
    {
        var path = FilePath;
        if (!IsModified || string.IsNullOrEmpty(path))
            return;

        // Another program changed the file since the editor read it: ask before overwriting its changes
        var current = FileContentSignature.Capture(path);
        if (current.Exists && current != signature)
        {
            var dialogService = ServiceProvider.Get<IDialogService>();
            var message = string.Format(Tr._p("Message", "The file {0} changed on disk since it was opened.\r\n\r\nDo you want to overwrite it with your changes?"), Path.GetFileName(path));
            var result = await dialogService.MessageBoxAsync(message, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result != MessageBoxResult.OK)
                return;
        }

        // The user can keep typing while the file is written: what is saved is the text of now
        var text = currentText;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, text, EncodingOf(path));

        Asset.Asset.Text = text;
        savedText = text;
        signature = FileContentSignature.Capture(path);
        IsModified = !string.Equals(currentText, savedText, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Unsaved edits are never dropped silently: with <paramref name="save"/> <c>null</c>, the user saves them, drops them, or
    /// keeps the editor open. The editor stays open when the save did not happen (the user kept a change of another program).
    /// </remarks>
    public override async Task<bool> PreviewCloseAsync(bool? save)
    {
        if (!IsModified || save == false)
            return true;

        if (save is null)
        {
            var buttons = new[]
            {
                new DialogButtonInfo { Content = Tr._p("Button", "Save"), Result = (int)MessageBoxResult.Yes, IsDefault = true },
                new DialogButtonInfo { Content = Tr._p("Button", "Don't save"), Result = (int)MessageBoxResult.No },
                new DialogButtonInfo { Content = Tr._p("Button", "Cancel"), Result = (int)MessageBoxResult.Cancel, IsCancel = true },
            };
            var message = string.Format(Tr._p("Message", "The script {0} has unsaved changes.\r\n\r\nDo you want to save them?"), Asset.Name);
            var result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, buttons, MessageBoxImage.Question);
            if (result == (int)MessageBoxResult.No)
                return true;
            if (result != (int)MessageBoxResult.Yes)
                return false;
        }

        try
        {
            await SaveAsync();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            var message = string.Format(Tr._p("Message", "The script {0} could not be saved.\r\n\r\n{1}"), Asset.Name, e.Message);
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        return !IsModified;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A synchronous close can neither save nor ask: the editor closes only when no edit would be lost, see
    /// <see cref="PreviewCloseAsync"/>.
    /// </remarks>
    public override bool PreviewClose(bool? save)
    {
        return !IsModified || save == false;
    }

    /// <summary>
    /// The encoding to save a file with: UTF-8, with a byte order mark unless the file has none, so that a save changes only
    /// the text. A new file gets one, as the session writes source files.
    /// </summary>
    private static Encoding EncodingOf(string path)
    {
        if (!File.Exists(path))
            return Encoding.UTF8;

        Span<byte> start = stackalloc byte[3];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var length = stream.ReadAtLeast(start, start.Length, throwOnEndOfStream: false);
        var hasByteOrderMark = start[..length].SequenceEqual(Encoding.UTF8.Preamble);
        return hasByteOrderMark ? Encoding.UTF8 : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
