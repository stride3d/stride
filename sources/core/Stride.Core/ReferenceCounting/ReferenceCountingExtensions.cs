// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;

namespace Stride.Core.ReferenceCounting
{
    internal static class ReferenceCountingExtensions
    {
        /// <summary>
        /// Increments the reference count of this instance.
        /// </summary>
        /// <returns>The method returns the new reference count.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AddReferenceInternal([NotNull] this IReferencable referencable)
        {
            return referencable.AddReference();
        }

        /// <summary>
        /// Decrements the reference count of this instance.
        /// </summary>
        /// <returns>The method returns the new reference count.</returns>
        /// <remarks>When the reference count is going to 0, the component should release/dispose dependents objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReleaseInternal([NotNull] this IReferencable referencable)
        {
            return referencable.Release();
        }
    }
}
