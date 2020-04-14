// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Presentation.Tests.Dirtiables
{
    public class SimpleDirtyingOperation : Operation, IDirtyingOperation
    {
        private static int counter;

        public SimpleDirtyingOperation([NotNull] IEnumerable<IDirtiable> dirtiables)
        {
            Counter = ++counter;
            Dirtiables = dirtiables.ToList();
        }

        public int Counter { get; }

        /// <inheritdoc/>
        public bool IsDone { get; private set; } = true;

        /// <inheritdoc/>
        public IReadOnlyList<IDirtiable> Dirtiables { get; }

        public Action OnUndo { get; set; }

        public Action OnRedo { get; set; }

        /// <inheritdoc/>
        protected override void FreezeContent()
        {
            Console.WriteLine($"Freezed {this}");
        }

        /// <inheritdoc/>
        protected override void Rollback()
        {
            Console.WriteLine($"Rollbacking {this}");
            IsDone = false;
            OnUndo?.Invoke();
            Console.WriteLine($"Rollbacked {this}");
        }

        /// <inheritdoc/>
        protected override void Rollforward()
        {
            Console.WriteLine($"Rollforwarding {this}");
            IsDone = true;
            OnRedo?.Invoke();
            Console.WriteLine($"Rollforwarded {this}");
        }

        public override string ToString()
        {
            return $"Operation {Counter} (currently {(IsDone ? "done" : "undone")})";
        }
    }
}
