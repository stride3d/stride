// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Markup.Xaml;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

public sealed class GuidExtension : MarkupExtension
{
    public Guid Value { get; set; }

    public GuidExtension()
    {
        Value = Guid.Empty;
    }

    public GuidExtension(object value)
    {
        _ = Guid.TryParse(value as string, out var guid);
        Value = guid;
    }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Value;
    }
}
