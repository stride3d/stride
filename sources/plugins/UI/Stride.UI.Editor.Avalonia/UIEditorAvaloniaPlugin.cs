// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Markup.Xaml.Styling;
using Stride.Core.Assets.Editor.Services;
using Stride.Editor.Avalonia;

namespace Stride.UI.Editor.Avalonia.Views;

/// <summary>
/// The Avalonia editor's entry point into this package: the UI property templates (thickness, strip definition, rotation)
/// join the property grid, the control themes of this assembly join the application's resources, and the shortcuts of the UI
/// editors join the command registry.
/// </summary>
public sealed class UIEditorAvaloniaPlugin : StrideAssetsAvaloniaPlugin
{
    /// <summary>
    /// The dictionary of the control themes of this assembly.
    /// </summary>
    public static readonly Uri ThemesUri = new("avares://Stride.UI.Editor.Avalonia/Themes/Generic.axaml");

    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        RegisterResourceDictionary(context, new UIPropertyTemplates());
        RegisterControlThemes(context.Logger, new ResourceInclude(ThemesUri) { Source = ThemesUri });

        // The shortcuts of the UI editors
        foreach (var command in UIEditorCommands.Create())
            context.Commands.Register(command);
    }
}
