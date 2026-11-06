// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

public sealed class IntExtension : MarkupExtension
{
    [Content]
    public int Value { get; set; }

    public IntExtension()
    {
        Value = 0;
    }

    public IntExtension(object value)
    {
        Value = Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }
    
    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Value;
    }
}
