// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

public sealed class ThicknessExtension : MarkupExtension
{
    public ThicknessExtension(double uniformLength)
    {
        Value = new Thickness(uniformLength);
    }

    public ThicknessExtension(double horizontal, double vertical)
    {
        Value = new Thickness(horizontal, vertical, horizontal, vertical);
    }

    public ThicknessExtension(double left, double top, double right, double bottom)
    {
        Value = new Thickness(left, top, right, bottom);
    }

    public ThicknessExtension(Thickness value)
    {
        Value = value;
    }

    public ThicknessExtension()
    {
        Value = new Thickness(0.0);
    }

    [Content]
    public Thickness Value { get; set; }
    
    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Value;
    }
}
