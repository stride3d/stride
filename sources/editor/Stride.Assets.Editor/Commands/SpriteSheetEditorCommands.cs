// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Commands;

/// <summary>
/// The shortcuts of the sprite sheet editor, as commands of the registry scoped to it (<see cref="CommandDescriptor.GestureScope"/>):
/// they work only while the focus is in a sprite sheet editor, before the main window's, and the user can change them. Each
/// one runs the existing command of the editor it was invoked in (<see cref="CommandContext.Editor"/>), as its key did.
/// </summary>
/// <remarks>
/// The editor's default plugin registers them (<c>StrideEditorPlugin</c>). The keys of the pixel tools while dragging in the
/// image are not commands.
/// </remarks>
[Experimental("STREXP003")]
public static class SpriteSheetEditorCommands
{
    /// <summary>Selects the previous sprite (Ctrl+J).</summary>
    public const string PreviousSpriteId = "Stride.SpriteSheet.PreviousSprite";

    /// <summary>Selects the next sprite (Ctrl+K).</summary>
    public const string NextSpriteId = "Stride.SpriteSheet.NextSprite";

    /// <summary>Adds an image to the sprite sheet (Ctrl+U).</summary>
    public const string AddImageId = "Stride.SpriteSheet.AddImage";

    /// <summary>Duplicates the selected sprites (Ctrl+D).</summary>
    public const string DuplicateId = "Stride.SpriteSheet.Duplicate";

    /// <summary>Gives the selected sprites the whole image as their region (Ctrl+W).</summary>
    public const string UseWholeImageId = "Stride.SpriteSheet.UseWholeImage";

    /// <summary>Turns the magic wand tool on or off (M).</summary>
    public const string ToggleMagicWandId = "Stride.SpriteSheet.ToggleMagicWand";

    /// <summary>Frames the region of the selected sprite (F).</summary>
    public const string FocusOnRegionId = "Stride.SpriteSheet.FocusOnRegion";

    /// <summary>
    /// Creates the commands, each time new descriptors (a descriptor is registered once).
    /// </summary>
    public static IReadOnlyList<CommandDescriptor> Create() =>
    [
        Wrap(PreviousSpriteId, () => Tr._p("Menu", "Select previous sprite"), "Ctrl+J", x => x.SelectNextSpriteCommand, -1, null, "navigate", "back"),
        Wrap(NextSpriteId, () => Tr._p("Menu", "Select next sprite"), "Ctrl+K", x => x.SelectNextSpriteCommand, 1, null, "navigate", "forward"),
        Wrap(AddImageId, () => Tr._p("Menu", "Add image"), "Ctrl+U", x => x.AddNewImageCommand, null, null, "new", "sprite", "texture"),
        Wrap(DuplicateId, () => Tr._p("Menu", "Duplicate selected sprites"), "Ctrl+D", x => x.DuplicateSelectedImagesCommand, null, null, "copy", "clone"),
        Wrap(UseWholeImageId, () => Tr._p("Menu", "Use whole image"), "Ctrl+W", x => x.UseWholeImageCommand, null, null, "region", "texture"),
        Wrap(ToggleMagicWandId, () => Tr._p("Menu", "Magic wand"), "M", x => x.ToggleUseMagicWandCommand, null, x => x.ToolMode == SpriteSheetEditorToolMode.MagicWand, "tool", "region", "select"),
        Wrap(FocusOnRegionId, () => Tr._p("Menu", "Focus on region"), "F", x => x.FocusOnRegionCommand, null, null, "frame", "zoom"),
    ];

    private static CommandDescriptor Wrap(string id, Func<string> title, string gesture, Func<SpriteSheetEditorViewModel, ICommand> command, object? parameter, Func<SpriteSheetEditorViewModel, bool>? isChecked, params string[] keywords) => new()
    {
        Id = id,
        Category = CommandCategories.SpriteSheet,
        Title = title,
        DefaultGesture = gesture,
        GestureScope = typeof(SpriteSheetEditorViewModel),
        Keywords = keywords,
        IsChecked = isChecked is null ? null : context => context.Editor is SpriteSheetEditorViewModel editor && isChecked(editor),
        CanExecute = context => context.Editor is SpriteSheetEditorViewModel editor && command(editor).CanExecute(parameter),
        ExecuteAsync = context =>
        {
            if (context.Editor is SpriteSheetEditorViewModel editor && command(editor) is { } target && target.CanExecute(parameter))
                target.Execute(parameter);
            return Task.CompletedTask;
        },
    };
}
