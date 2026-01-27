// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Silk.NET.Core.Native;

namespace Stride.Graphics;

/// <summary>
///   Provides a mechanism for comparing two <see cref="ComPtr{T}"/> instances for equality.
/// </summary>
/// <typeparam name="T">
///   The type of the unmanaged COM interface that the <see cref="ComPtr{T}"/> instances point to.
/// </typeparam>
internal sealed class ComPtrEqualityComparer<T> : IEqualityComparer<ComPtr<T>>
                where T : unmanaged, IComVtbl<T>
{
    /// <summary>
    ///   Provides a default instance of the <see cref="ComPtrEqualityComparer{T}"/> class.
    /// </summary>
    public static ComPtrEqualityComparer<T> Default = new();

    private ComPtrEqualityComparer() { }

    /// <inheritdoc/>
    public unsafe bool Equals(ComPtr<T> x, ComPtr<T> y) => x.Handle == y.Handle;

    /// <inheritdoc/>
    public unsafe int GetHashCode([DisallowNull] ComPtr<T> obj) => ((nint) obj.Handle).GetHashCode();
}

#endif
