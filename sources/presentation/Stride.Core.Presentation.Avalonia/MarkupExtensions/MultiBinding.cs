using Avalonia.Data;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

/// <summary>
/// This class extends the <see cref="global::Avalonia.Data.MultiBinding"/> by providing constructors that allows construction using markup extension.
/// </summary>
public sealed class MultiBinding : global::Avalonia.Data.MultiBinding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBinding"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2)
        : this(binding1, binding2, null, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBinding"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2, IBinding binding3)
        : this(binding1, binding2, binding3, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBinding"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2, IBinding binding3, IBinding binding4)
        : this(binding1, binding2, binding3, binding4, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBinding"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    /// <param name="binding5">The fifth binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2, IBinding binding3, IBinding binding4, IBinding binding5)
        : this(binding1, binding2, binding3, binding4, binding5, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBinding"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    /// <param name="binding5">The fifth binding.</param>
    /// <param name="binding6">The sixth binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2, IBinding binding3, IBinding binding4, IBinding binding5, IBinding binding6)
        : this(binding1, binding2, binding3, binding4, binding5, binding6, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBinding"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    /// <param name="binding5">The fifth binding.</param>
    /// <param name="binding6">The sixth binding.</param>
    /// <param name="binding7">The seventh binding.</param>
    public MultiBinding(IBinding binding1, IBinding binding2, IBinding binding3, IBinding binding4, IBinding binding5, IBinding binding6, IBinding binding7)
        : this(binding1, binding2, binding3, binding4, binding5, binding6, binding7, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBinding"/> class.
    /// </summary>
    /// <param name="binding1">The first binding.</param>
    /// <param name="binding2">The second binding.</param>
    /// <param name="binding3">The third binding.</param>
    /// <param name="binding4">The fourth binding.</param>
    /// <param name="binding5">The fifth binding.</param>
    /// <param name="binding6">The sixth binding.</param>
    /// <param name="binding7">The seventh binding.</param>
    /// <param name="binding8">The eighth binding.</param>
    public MultiBinding(IBinding? binding1, IBinding? binding2, IBinding? binding3, IBinding? binding4, IBinding? binding5, IBinding? binding6, IBinding? binding7, IBinding? binding8)
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

    public MultiBinding ProvideTypedValue() => this;
}
