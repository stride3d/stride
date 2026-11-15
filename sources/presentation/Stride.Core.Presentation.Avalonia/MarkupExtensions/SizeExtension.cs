// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

public sealed class SizeExtension : MarkupExtension
{
    public SizeExtension(double uniformLength)
    {
        Value = new Size(uniformLength, uniformLength);
    }

    public SizeExtension(double width, double height)
    {
        Value = new Size(width, height);
    }

    public SizeExtension(Size value)
    {
        Value = value;
    }

    public SizeExtension()
    {
        Value = Size.Infinity;
    }

    [Content]
    public Size Value { get; set; }
    
    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Value;
    }
}
