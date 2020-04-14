// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Transactions
{
    public interface IMergeableOperation
    {
        /// <summary>
        /// Indicates whether the given operation can be merged into this operation.
        /// </summary>
        /// <param name="otherOperation">The operation to merge into this operation.</param>
        /// <returns><c>True</c> if the operation can be merged, <c>False</c> otherwise.</returns>
        /// <remarks>The operation given as argument is supposed to have occurred after this one.</remarks>
        bool CanMerge(IMergeableOperation otherOperation);

        /// <summary>
        /// Merges the given operation into this operation.
        /// </summary>
        /// <param name="otherOperation">The operation to merge into this operation.</param>
        /// <remarks>The operation given as argument is supposed to have occurred after this one.</remarks>
        void Merge(Operation otherOperation);
    }
}
