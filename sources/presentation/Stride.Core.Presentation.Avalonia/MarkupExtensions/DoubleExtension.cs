// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

public sealed class DoubleExtension : MarkupExtension
{
    [Content]
    public double Value { get; set; }

    public DoubleExtension()
    {
        Value = 0.0;
    }

    public DoubleExtension(object value)
    {
        Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }
    
    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Value;
    }
}
