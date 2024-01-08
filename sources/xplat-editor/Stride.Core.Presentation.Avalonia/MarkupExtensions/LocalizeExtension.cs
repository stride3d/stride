// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Stride.Core.Translation;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

public sealed class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension()
    {
    }

    public LocalizeExtension(object value)
    {
        Text = value?.ToString();
    }

    /// <summary>
    /// Localization context or <c>null</c>.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// The text to localize.
    /// </summary>
    [Content]
    public string? Text { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Text))
            return string.Empty;

        // FIXME xplat-editor original version was attempting to load some assembly.
        //       Why? Was it because classes that were already translated might have changed namespaces?
        //       Note: might need to strip "Avalonia" from the name.
        var assembly = Assembly.GetCallingAssembly();

        return string.IsNullOrEmpty(Context)
            ? TranslationManager.Instance.GetString(Text, assembly)
            : TranslationManager.Instance.GetParticularString(Context, Text, assembly);
    }
}
