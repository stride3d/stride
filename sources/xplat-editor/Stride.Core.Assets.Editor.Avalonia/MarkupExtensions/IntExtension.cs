// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Markup.Xaml;

namespace Stride.Core.Assets.Editor.Avalonia.MarkupExtensions;

public class IntExtension : MarkupExtension
{
    public int Value { get; set; }

    public IntExtension(object value)
    {
        Value = Convert.ToInt32(value);
    }
    
    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Value;
    }
}
