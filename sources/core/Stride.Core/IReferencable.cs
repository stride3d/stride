// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core
{
    /// <summary>
    /// Base interface for all referencable objects.
    /// </summary>
    public interface IReferencable
    {
        /// <summary>
        /// Gets the reference count of this instance.
        /// </summary>
        /// <value>
        /// The reference count.
        /// </value>
        int ReferenceCount { get; }

        /// <summary>
        /// Increments the reference count of this instance.
        /// </summary>
        /// <returns>The method returns the new reference count.</returns>
        int AddReference();

        /// <summary>
        /// Decrements the reference count of this instance.
        /// </summary>
        /// <returns>The method returns the new reference count.</returns>
        /// <remarks>When the reference count is going to 0, the component should release/dispose dependents objects.</remarks>
        int Release();
    }
}
