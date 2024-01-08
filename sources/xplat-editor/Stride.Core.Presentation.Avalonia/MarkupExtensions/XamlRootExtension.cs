// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Markup.Xaml;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

/// <summary>
/// Finds and returns the root object of the current XAML document.
/// </summary>
public sealed class XamlRootExtension : MarkupExtension
{
    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
        return provider?.RootObject ?? AvaloniaProperty.UnsetValue;
    }
}
