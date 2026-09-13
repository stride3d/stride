// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

/// <summary>
/// This markup extension allows to create a <see cref="Key"/> instance from a string representing the key.
/// </summary>
public sealed class KeyExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    [Content]
    public Key Key { get; set; }

    public KeyExtension()
    {
        Key = Key.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyExtension"/> class with a string representing the key.
    /// </summary>
    /// <param name="key">A string representing the key.</param>
    public KeyExtension(string key)
    {
        Key = (Key)Enum.Parse(typeof(Key), key, true);
    }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Key;
    }
}
