using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Metadata;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

/// <summary>
/// This class augments the <see cref="MultiBinding"/> by providing constructors that allows construction using markup extension.
/// </summary>
public sealed class MultiBindingExtension
{
    private MultiBinding? cachedBinding;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBindingExtension"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    public MultiBindingExtension(BindingBase binding1, BindingBase binding2)
        : this(binding1, binding2, null, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBindingExtension"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    public MultiBindingExtension(BindingBase binding1, BindingBase binding2, BindingBase binding3)
        : this(binding1, binding2, binding3, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBindingExtension"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    public MultiBindingExtension(BindingBase binding1, BindingBase binding2, BindingBase binding3, BindingBase binding4)
        : this(binding1, binding2, binding3, binding4, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBindingExtension"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    /// <param name="binding5">The fifth binding.</param>
    public MultiBindingExtension(BindingBase binding1, BindingBase binding2, BindingBase binding3, BindingBase binding4, BindingBase binding5)
        : this(binding1, binding2, binding3, binding4, binding5, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBindingExtension"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    /// <param name="binding5">The fifth binding.</param>
    /// <param name="binding6">The sixth binding.</param>
    public MultiBindingExtension(BindingBase binding1, BindingBase binding2, BindingBase binding3, BindingBase binding4, BindingBase binding5, BindingBase binding6)
        : this(binding1, binding2, binding3, binding4, binding5, binding6, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBindingExtension"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    /// <param name="binding5">The fifth binding.</param>
    /// <param name="binding6">The sixth binding.</param>
    /// <param name="binding7">The seventh binding.</param>
    public MultiBindingExtension(BindingBase binding1, BindingBase binding2, BindingBase binding3, BindingBase binding4, BindingBase binding5, BindingBase binding6, BindingBase binding7)
        : this(binding1, binding2, binding3, binding4, binding5, binding6, binding7, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBindingExtension"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    /// <param name="binding5">The fifth binding.</param>
    /// <param name="binding6">The sixth binding.</param>
    /// <param name="binding7">The seventh binding.</param>
    /// <param name="binding8">The eighth binding.</param>
    public MultiBindingExtension(BindingBase? binding1, BindingBase? binding2, BindingBase? binding3, BindingBase? binding4, BindingBase? binding5, BindingBase? binding6, BindingBase? binding7, BindingBase? binding8)
        : base()
    {
        if (binding1 != null) Bindings.Add(binding1);
        if (binding2 != null) Bindings.Add(binding2);
        if (binding3 != null) Bindings.Add(binding3);
        if (binding4 != null) Bindings.Add(binding4);
        if (binding5 != null) Bindings.Add(binding5);
        if (binding6 != null) Bindings.Add(binding6);
        if (binding7 != null) Bindings.Add(binding7);
        if (binding8 != null) Bindings.Add(binding8);
    }

    [Content, AssignBinding]
    public IList<BindingBase> Bindings { get; init; } = [];

    public IMultiValueConverter? Converter { get; init; }

    public MultiBinding ProvideTypedValue()
    {
        if (cachedBinding is not null)
            return cachedBinding;

        cachedBinding = new MultiBinding { Converter = Converter };

        foreach (var binding in Bindings)
            cachedBinding.Bindings.Add(binding);

        return cachedBinding;
    }
}
