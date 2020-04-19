// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Transactions;

namespace Stride.Core.Design.Tests.Transactions
{
    public class TestTransaction
    {
        [Fact]
        public void TestEmptyTransaction()
        {
            var stack = TransactionStackFactory.Create(5);
            using (stack.CreateTransaction())
            {
                // Empty transaction
            }
            Assert.True(stack.IsEmpty);
            Assert.False(stack.CanRollback);
            Assert.False(stack.CanRollforward);
            Assert.Throws<TransactionException>(() => stack.Rollback());
        }

        [Fact]
        public void TestEmptyNestedTransaction()
        {
            var stack = TransactionStackFactory.Create(5);
            using (stack.CreateTransaction())
            {
                using (stack.CreateTransaction())
                {
                    // Empty transaction
                }
            }

            Assert.True(stack.IsEmpty);
            Assert.False(stack.CanRollback);
            Assert.False(stack.CanRollforward);
            Assert.Throws<TransactionException>(() => stack.Rollback());
        }

        [Fact]
        public void TestSingleOperationTransaction()
        {
            var stack = TransactionStackFactory.Create(5);
            SimpleOperation operation;
            using (stack.CreateTransaction())
            {
                operation = new SimpleOperation();
                stack.PushOperation(new SimpleOperation());
            }
            Assert.False(stack.IsEmpty);
            Assert.True(stack.CanRollback);
            Assert.False(stack.CanRollforward);
            Assert.True(operation.IsDone);
            Assert.Equal(0, operation.RollbackCount);
            Assert.Equal(0, operation.RollforwardCount);
        }

        [Fact]
        public void TestSingleOperationTransactionRollback()
        {
            var stack = TransactionStackFactory.Create(5);
            SimpleOperation operation;
            using (stack.CreateTransaction())
            {
                operation = new SimpleOperation();
                stack.PushOperation(operation);
            }
            // Above code must be similar to TestSingleOperationTransaction
            stack.Rollback();
            Assert.False(stack.IsEmpty);
            Assert.False(stack.CanRollback);
            Assert.True(stack.CanRollforward);
            Assert.False(operation.IsDone);
            Assert.Equal(1, operation.RollbackCount);
            Assert.Equal(0, operation.RollforwardCount);
        }

        [Fact]
        public void TestSingleOperationTransactionRollforward()
        {
            var stack = TransactionStackFactory.Create(5);
            SimpleOperation operation;
            using (stack.CreateTransaction())
            {
                operation = new SimpleOperation();
                stack.PushOperation(operation);
            }
            stack.Rollback();
            // Above code must be similar to TestSingleOperationTransactionRollback
            stack.Rollforward();
            Assert.False(stack.IsEmpty);
            Assert.True(stack.CanRollback);
            Assert.False(stack.CanRollforward);
            Assert.True(operation.IsDone);
            Assert.Equal(1, operation.RollbackCount);
            Assert.Equal(1, operation.RollforwardCount);
        }

        [Fact]
        public void TestMultipleOperationsTransaction()
        {
            var stack = TransactionStackFactory.Create(5);
            var counter = new OrderedOperation.Counter();
            OrderedOperation[] operations = new OrderedOperation[4];
            using (stack.CreateTransaction())
            {
                for (int i = 0; i < operations.Length; ++i)
                {
                    var operation = new OrderedOperation(counter, 0, operations.Length - i - 1);
                    stack.PushOperation(operation);
                }
            }
            Assert.False(stack.IsEmpty);
            Assert.True(stack.CanRollback);
            Assert.False(stack.CanRollforward);
        }

        [Fact]
        public void TestMultipleOperationsTransactionRollback()
        {
            var stack = TransactionStackFactory.Create(5);
            var counter = new OrderedOperation.Counter();
            OrderedOperation[] operations = new OrderedOperation[4];
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < operations.Length; ++i)
                {
                    operations[i] = new OrderedOperation(counter, i, operations.Length);
                    stack.PushOperation(operations[i]);
                }
            }
            // Above code must be similar to TestMultipleOperationsTransaction
            stack.Rollback();
            Assert.False(stack.IsEmpty);
            Assert.False(stack.CanRollback);
            Assert.True(stack.CanRollforward);
            Assert.Equal(operations.Length, counter.Value);
            foreach (var operation in operations)
            {
                Assert.False(operation.IsDone);
                Assert.Equal(1, operation.RollbackCount);
                Assert.Equal(0, operation.RollforwardCount);
            }
        }

        [Fact]
        public void TestMultipleOperationsTransactionRollforward()
        {
            var stack = TransactionStackFactory.Create(5);
            var counter = new OrderedOperation.Counter();
            OrderedOperation[] operations = new OrderedOperation[4];
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < operations.Length; ++i)
                {
                    operations[i] = new OrderedOperation(counter, i, operations.Length);
                    stack.PushOperation(operations[i]);
                }
            }
            stack.Rollback();
            // Above code must be similar to TestMultipleOperationsTransactionRollback
            counter.Reset();
            stack.Rollforward();
            Assert.False(stack.IsEmpty);
            Assert.True(stack.CanRollback);
            Assert.False(stack.CanRollforward);
            Assert.Equal(operations.Length, counter.Value);
            foreach (var operation in operations)
            {
                Assert.True(operation.IsDone);
                Assert.Equal(1, operation.RollbackCount);
                Assert.Equal(1, operation.RollforwardCount);
            }
        }

        [Fact]
        public void TestClear()
        {
            var stack = TransactionStackFactory.Create(5);
            var operations = new SimpleOperation[4];
            for (var i = 0; i < operations.Length; ++i)
            {
                using (stack.CreateTransaction())
                {
                    operations[i] = new SimpleOperation();
                    stack.PushOperation(operations[i]);
                }
            }
            stack.Clear();
            Assert.False(stack.CanRollback);
            Assert.False(stack.CanRollforward);
            Assert.Equal(5, stack.Capacity);
            Assert.True(stack.IsEmpty);
            Assert.False(stack.IsFull);
            foreach (var operation in operations)
            {
                Assert.True(operation.IsFrozen);
            }
        }

        [Fact]
        public void TestDiscardStackFull()
        {
            var stack = TransactionStackFactory.Create(5);
            var operations = new SimpleOperation[6];
            for (var i = 0; i < operations.Length; ++i)
            {
                using (stack.CreateTransaction())
                {
                    operations[i] = new SimpleOperation();
                    stack.PushOperation(operations[i]);
                }
            }

            Assert.True(operations[0].IsFrozen);
            for (var i = 1; i < operations.Length; ++i)
            {
                Assert.False(operations[i].IsFrozen);
                Assert.Equal(operations[i], ((TransactionStack)stack).Transactions[i - 1].Operations[0]);
            }
        }

        [Fact]
        public void TestDiscardMultipleStackFull()
        {
            var stack = TransactionStackFactory.Create(5);
            var operations = new SimpleOperation[8];
            for (var i = 0; i < operations.Length; ++i)
            {
                using (stack.CreateTransaction())
                {
                    operations[i] = new SimpleOperation();
                    stack.PushOperation(operations[i]);
                }
            }

            for (int i = 0; i < 3; ++i)
            {
                Assert.True(operations[0].IsFrozen);
            }
            for (var i = 3; i < operations.Length; ++i)
            {
                Assert.False(operations[i].IsFrozen);
                Assert.Equal(operations[i], ((TransactionStack)stack).Transactions[i - 3].Operations[0]);
            }
        }

        [Fact]
        public void TestDiscardOnePurged()
        {
            var stack = TransactionStackFactory.Create(5);
            var operations = new SimpleOperation[6];
            for (var i = 0; i < operations.Length - 1; ++i)
            {
                using (stack.CreateTransaction())
                {
                    operations[i] = new SimpleOperation();
                    stack.PushOperation(operations[i]);
                }
            }
            stack.Rollback();
            using (stack.CreateTransaction())
            {
                operations[5] = new SimpleOperation();
                stack.PushOperation(operations[5]);
            }

            for (var i = 0; i < 4; ++i)
            {
                Assert.False(operations[i].IsFrozen);
                Assert.Equal(operations[i], ((TransactionStack)stack).Transactions[i].Operations[0]);
            }
            // operations[4] is the discarded transaction
            Assert.True(operations[4].IsFrozen);
            Assert.False(operations[5].IsFrozen);
            Assert.Equal(operations[5], ((TransactionStack)stack).Transactions[4].Operations[0]);
        }
    }
}
