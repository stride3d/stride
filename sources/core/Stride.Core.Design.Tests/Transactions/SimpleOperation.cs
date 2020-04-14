// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Transactions;

namespace Stride.Core.Design.Tests.Transactions
{
    internal class SimpleOperation : Operation
    {
        public Guid Guid { get; } = Guid.NewGuid();

        public bool IsDone { get; private set; } = true;

        public int RollbackCount { get; private set; }

        public int RollforwardCount { get; private set; }

        protected override void Rollback()
        {
            IsDone = false;
            ++RollbackCount;
        }

        protected override void Rollforward()
        {
            IsDone = true;
            ++RollforwardCount;
        }
    }
}
