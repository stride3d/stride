// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Dirtiables
{
    /// <summary>
    /// A snapshot of all operations that are currently done.
    /// </summary>
    /// <seealso cref="IDirtyingOperation.IsDone"/>
    public class DirtiableSnapshot
    {
        private readonly HashSet<IDirtyingOperation> operationsDone;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirtiableSnapshot"/> class.
        /// </summary>
        /// <param name="operations">A collection of <see cref="IDirtyingOperation"/> instances from which to extract those who are currently done.</param>
        /// <seealso cref="IDirtyingOperation.IsDone"/>
        public DirtiableSnapshot([NotNull] IEnumerable<IDirtyingOperation> operations)
        {
            operationsDone = new HashSet<IDirtyingOperation>(operations.Where(x => x.IsDone));
        }

        /// <summary>
        /// Indicates whether the given operation is contained in this snapshot as an operation that was done during the creation of the snapshot.
        /// </summary>
        /// <param name="operation">The operation to check.</param>
        /// <returns><c>True</c> if it was done during at the creation of this snapshot, false otherwise.</returns>
        /// <seealso cref="IDirtyingOperation.IsDone"/>
        public bool Contains(IDirtyingOperation operation) => operationsDone.Contains(operation);
    }
}
