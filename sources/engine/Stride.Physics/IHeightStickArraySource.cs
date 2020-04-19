// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics
{
    public interface IHeightStickArraySource : IHeightStickParameters
    {
        /// <summary>
        /// The size of the source.
        /// </summary>
        /// <remarks>
        /// X is width and Y is length.
        /// They should be greater than or equal to 2.
        /// For example, this size should be 65 * 65 when you want 64 * 64 size in a scene.
        /// </remarks>
        Int2 HeightStickSize { get; }

        /// <summary>
        /// Copy the source data to the height stick array.
        /// </summary>
        /// <typeparam name="T">The data type of the height</typeparam>
        /// <param name="heightStickArray">The destination to copy the data.</param>
        /// <param name="index">The start index of the destination to copy the data.</param>
        void CopyTo<T>(UnmanagedArray<T> heightStickArray, int index) where T : struct;

        bool IsValid();

        bool Match(object obj);
    }
}
