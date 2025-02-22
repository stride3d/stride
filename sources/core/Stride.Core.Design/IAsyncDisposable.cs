// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core;

/// <summary>
/// An interface allowing to dispose an object asynchronously.
/// </summary>
[Obsolete("Use IAsyncDisposable from System")]
public interface IAsyncDisposable
{
    /// <summary>
    /// Disposes the given instance asynchronously.
    /// </summary>
    /// <returns>A task that completes when this instance has been disposed.</returns>
    Task DisposeAsync();
}
