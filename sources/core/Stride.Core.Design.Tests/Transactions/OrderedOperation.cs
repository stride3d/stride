// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

namespace Stride.Core.Design.Tests.Transactions
{
    internal class OrderedOperation : SimpleOperation
    {
        private readonly Counter counter;
        private readonly int order;
        private readonly int totalCount;

        internal class Counter
        {
            public void Reset() => Value = 0;
            public int Value { get; set; }
        }

        public OrderedOperation(Counter counter, int order, int totalCount)
        {
            this.counter = counter;
            this.order = order;
            this.totalCount = totalCount;
        }

        protected override void Rollback()
        {
            // Rollback is done in reverse order
            var value = totalCount - order - 1;
            Assert.Equal(value, counter.Value);
            counter.Value++;
            base.Rollback();
        }

        protected override void Rollforward()
        {
            Assert.Equal(order, counter.Value);
            counter.Value++;
            base.Rollforward();
        }
    }
}
