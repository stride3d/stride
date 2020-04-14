// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Transactions;

namespace Stride.Core.Presentation.Dirtiables
{
    public abstract class DirtyingOperation : Operation, IDirtyingOperation
    {
        protected DirtyingOperation([NotNull] IEnumerable<IDirtiable> dirtiables)
        {
            if (dirtiables == null) throw new ArgumentNullException(nameof(dirtiables));
            IsDone = true;
            Dirtiables = dirtiables.ToList();
        }

        /// <inheritdoc/>
        public bool IsDone { get; private set; }

        /// <inheritdoc/>
        public override bool HasEffect => true;

        /// <inheritdoc/>
        public IReadOnlyList<IDirtiable> Dirtiables { get; }

        /// <summary>
        /// Indicates whether this operation affects the same dirtiable objects that the given operation.
        /// </summary>
        /// <param name="otherOperation">The operation for which to compare dirtiables.</param>
        /// <returns><c>True</c> if this operation affects the same dirtiable objects that the given operation, <c>False</c> otherwise.</returns>
        public bool HasSameDirtiables([NotNull] DirtyingOperation otherOperation)
        {
            if (otherOperation.Dirtiables.Count != Dirtiables.Count)
                return false;

            foreach (var dirtiable in Dirtiables)
            {
                if (!otherOperation.Dirtiables.Contains(dirtiable))
                    return false;
            }
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{{GetType().Name}}}";
        }

        /// <inheritdoc/>
        protected sealed override void Rollback()
        {
            Undo();
            IsDone = false;
        }

        /// <inheritdoc/>
        protected sealed override void Rollforward()
        {
            Redo();
            IsDone = true;
        }

        protected abstract void Undo();

        protected abstract void Redo();
    }
}
