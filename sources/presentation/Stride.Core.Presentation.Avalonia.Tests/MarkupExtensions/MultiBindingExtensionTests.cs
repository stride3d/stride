// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Data;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Avalonia.MarkupExtensions;

namespace Stride.Core.Presentation.Avalonia.Tests.MarkupExtensions;

public sealed class MultiBindingExtensionTests
{
    [Fact]
    public void ProvidesACachedMultiBinding()
    {
        var extension = new MultiBindingExtension(new Binding("A"), new Binding("B")) { Converter = new AndMulti() };

        var binding = extension.ProvideTypedValue();

        Assert.Equal(2, binding.Bindings.Count);
        Assert.IsType<AndMulti>(binding.Converter);
        Assert.Same(binding, extension.ProvideTypedValue());
    }

    [Fact]
    public void FallbackValueIsUnsetByDefault()
    {
        var binding = new MultiBindingExtension(new Binding("A"), new Binding("B")).ProvideTypedValue();
        Assert.Same(AvaloniaProperty.UnsetValue, binding.FallbackValue);
    }

    [Fact]
    public void FallbackValueIsPassedToTheMultiBinding()
    {
        var binding = new MultiBindingExtension(new Binding("A"), new Binding("B")) { FallbackValue = false }.ProvideTypedValue();
        Assert.Equal(false, binding.FallbackValue);
    }
}
