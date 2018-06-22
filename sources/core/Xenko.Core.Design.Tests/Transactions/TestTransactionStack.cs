// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Xenko.Core.Transactions;

namespace Xenko.Core.Design.Tests.Transactions
{
    public class TestTransactionStack
    {
        [Fact]
        public void TestConstruction()
        {
            var stack = TransactionStackFactory.Create(5);
            Assert.Equal(false, stack.CanRollback);
            Assert.Equal(false, stack.CanRollforward);
            Assert.Equal(5, stack.Capacity);
            Assert.Equal(true, stack.IsEmpty);
            Assert.Equal(false, stack.IsFull);
        }

        [Fact]
        public void TestOverCapacity()
        {
            var stack = (TransactionStack)TransactionStackFactory.Create(5);
            var operations = new SimpleOperation[6];
            for (var i = 0; i < 5; ++i)
            {
                Assert.Equal(false, stack.IsFull);
                using (stack.CreateTransaction())
                {
                    operations[i] = new SimpleOperation();
                    stack.PushOperation(operations[i]);
                }
            }
            Assert.Equal(true, stack.IsFull);
            Assert.Equal(5, stack.Capacity);
            for (var i = 0; i < 5; ++i)
            {
                Assert.Equal(operations[i], ((Transaction)stack.Transactions[i]).Operations[0]);
            }
            using (stack.CreateTransaction())
            {
                operations[5] = new SimpleOperation();
                stack.PushOperation(operations[5]);
            }
            Assert.Equal(5, stack.Transactions.Count);
            Assert.Equal(5, stack.Capacity);
            Assert.Equal(true, operations[0].IsFrozen);
            for (var i = 0; i < 5; ++i)
            {
                Assert.Equal(operations[i+1], ((Transaction)stack.Transactions[i]).Operations[0]);
            }
        }

        [Fact]
        public void TestZeroCapacity()
        {
            var stack = (TransactionStack)TransactionStackFactory.Create(0);
            SimpleOperation operation;
            Assert.Equal(false, stack.CanRollback);
            Assert.Equal(false, stack.CanRollforward);
            Assert.Equal(0, stack.Capacity);
            Assert.Equal(true, stack.IsFull);
            Assert.Equal(true, stack.IsEmpty);

            using (stack.CreateTransaction())
            {
                operation = new SimpleOperation();
                stack.PushOperation(operation);
            }
            Assert.Equal(true, operation.IsFrozen);
            using (stack.CreateTransaction())
            {
                operation = new SimpleOperation();
                stack.PushOperation(operation);
            }
            Assert.Equal(true, operation.IsFrozen);
            Assert.Equal(false, stack.CanRollback);
            Assert.Equal(false, stack.CanRollforward);
            Assert.Equal(0, stack.Capacity);
            Assert.Equal(true, stack.IsFull);
            Assert.Equal(true, stack.IsEmpty);
        }

        [Fact]
        public void TestInterleavedTransactionThrows()
        {
            var stack = (TransactionStack)TransactionStackFactory.Create(5);
            SimpleOperation operation;

            // Root transaction
            var transaction1 = stack.CreateTransaction();
            operation = new SimpleOperation();
            stack.PushOperation(operation);

            // Nested transaction
            stack.CreateTransaction();
            operation = new SimpleOperation();
            stack.PushOperation(operation);

            // Complete root transaction
            Assert.Throws(typeof(TransactionException), () => transaction1.Complete());
        }

        [Fact]
        public void TestKeepParentAlive()
        {
            var stack = (TransactionStack)TransactionStackFactory.Create(5);
            SimpleOperation operation;

            // Root transaction
            var transaction1 = stack.CreateTransaction();
            operation = new SimpleOperation();
            stack.PushOperation(operation);

            // Nested transaction
            var transaction2 = stack.CreateTransaction(TransactionFlags.KeepParentsAlive);
            operation = new SimpleOperation();
            stack.PushOperation(operation);

            transaction1.Complete();

            // transaction1 is still kept alive by transaction2
            Assert.True(stack.TransactionInProgress);

            stack.PushOperation(operation);

            transaction2.Complete();

            // All transactions should be done now
            Assert.False(stack.TransactionInProgress);

            // And stack has one transaction
            Assert.False(stack.IsEmpty);
        }
    }
}
