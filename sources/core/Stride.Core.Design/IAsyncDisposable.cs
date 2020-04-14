// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

namespace Stride.Core
{
    /// <summary>
    /// An interface allowing to dispose an object asynchronously.
    /// </summary>
    public interface IAsyncDisposable
    {
        /// <summary>
        /// Disposes the given instance asynchronously.
        /// </summary>
        /// <returns>A task that completes when this instance has been disposed.</returns>
        Task DisposeAsync();
    }
}
