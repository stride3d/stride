// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.Transactions;
using Xenko.Core.Presentation.Dirtiables;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    internal class AssetsUpgradeOperation : Operation, IDirtyingOperation
    {
        public AssetsUpgradeOperation([NotNull] IDirtiable dirtiable)
        {
            if (dirtiable == null) throw new ArgumentNullException(nameof(dirtiable));
            Dirtiables = new [] {dirtiable};
        }

        /// <inheritdoc/>
        public bool IsDone => true;

        /// <inheritdoc/>
        public IReadOnlyList<IDirtiable> Dirtiables { get; }

        /// <inheritdoc/>
        protected override void Rollback()
        {
            // Intentionally does nothing
        }

        /// <inheritdoc/>
        protected override void Rollforward()
        {
            // Intentionally does nothing
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{{nameof(AssetsUpgradeOperation)}: {Dirtiables[0]}";
        }
    }
}
