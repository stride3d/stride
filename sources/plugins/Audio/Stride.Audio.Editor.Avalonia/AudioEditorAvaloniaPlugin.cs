// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Markup.Xaml.Styling;
using Stride.Core.Assets.Editor.Services;
using Stride.Editor.Avalonia;

namespace Stride.Audio.Editor;

/// <summary>
/// The Avalonia editor's entry point into this package: the preview views declared in this assembly register through it,
/// and their control themes join the application's resources.
/// </summary>
public sealed class AudioEditorAvaloniaPlugin : StrideAssetsAvaloniaPlugin
{
    /// <summary>
    /// The dictionary of the control themes of this assembly.
    /// </summary>
    public static readonly Uri ThemesUri = new("avares://Stride.Audio.Editor.Avalonia/Themes/Generic.axaml");

    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        RegisterControlThemes(context.Logger, new ResourceInclude(ThemesUri) { Source = ThemesUri });
    }
}
