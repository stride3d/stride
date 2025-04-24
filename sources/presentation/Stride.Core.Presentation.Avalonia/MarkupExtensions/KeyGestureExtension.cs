// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

/// <summary>
/// This markup extension allows to create a <see cref="KeyGesture"/> instance from a string representing the gesture.
/// </summary>
public sealed class KeyGestureExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the key gesture.
    /// </summary>
    [Content]
    public KeyGesture Gesture { get; set; }

    public KeyGestureExtension()
    {
        Gesture = new KeyGesture(Key.None);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyGestureExtension"/> class with a string representing the gesture.
    /// </summary>
    /// <param name="gesture">A string representing the gesture.</param>
    public KeyGestureExtension(string gesture)
    {
        var modifiers = KeyModifiers.None;
        var tokens = gesture.Split('+');
        for (var i = 0; i < tokens.Length - 1; ++i)
        {
            var token = tokens[i].Replace("Ctrl", "Control");
            var modifier = (KeyModifiers)Enum.Parse(typeof(KeyModifiers), token, true);
            modifiers |= modifier;
        }
        var key = (Key)Enum.Parse(typeof(Key), tokens[^1], true);
        Gesture = new KeyGesture(key, modifiers);
    }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Gesture;
    }
}
