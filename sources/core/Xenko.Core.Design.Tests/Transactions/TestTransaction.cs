// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;
using Xenko.Core.Transactions;

namespace Xenko.Core.Design.Tests.Transactions
{
    [TestFixture]
    public class TestTransaction
    {
        [Test]
        public void TestEmptyTransaction()
        {
            var stack = TransactionStackFactory.Create(5);
            using (stack.CreateTransaction())
            {
                // Empty transaction
            }
            Assert.AreEqual(true, stack.IsEmpty);
            Assert.AreEqual(false, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.Throws<TransactionException>(() => stack.Rollback());
        }

        [Test]
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

            Assert.AreEqual(true, stack.IsEmpty);
            Assert.AreEqual(false, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.Throws<TransactionException>(() => stack.Rollback());
        }

        [Test]
        public void TestSingleOperationTransaction()
        {
            var stack = TransactionStackFactory.Create(5);
            SimpleOperation operation;
            using (stack.CreateTransaction())
            {
                operation = new SimpleOperation();
                stack.PushOperation(new SimpleOperation());
            }
            Assert.AreEqual(false, stack.IsEmpty);
            Assert.AreEqual(true, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.AreEqual(true, operation.IsDone);
            Assert.AreEqual(0, operation.RollbackCount);
            Assert.AreEqual(0, operation.RollforwardCount);
        }

        [Test]
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
            Assert.AreEqual(false, stack.IsEmpty);
            Assert.AreEqual(false, stack.CanRollback);
            Assert.AreEqual(true, stack.CanRollforward);
            Assert.AreEqual(false, operation.IsDone);
            Assert.AreEqual(1, operation.RollbackCount);
            Assert.AreEqual(0, operation.RollforwardCount);
        }

        [Test]
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
            Assert.AreEqual(false, stack.IsEmpty);
            Assert.AreEqual(true, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.AreEqual(true, operation.IsDone);
            Assert.AreEqual(1, operation.RollbackCount);
            Assert.AreEqual(1, operation.RollforwardCount);
        }

        [Test]
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
            Assert.AreEqual(false, stack.IsEmpty);
            Assert.AreEqual(true, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
        }

        [Test]
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
            Assert.AreEqual(false, stack.IsEmpty);
            Assert.AreEqual(false, stack.CanRollback);
            Assert.AreEqual(true, stack.CanRollforward);
            Assert.AreEqual(operations.Length, counter.Value);
            foreach (var operation in operations)
            {
                Assert.AreEqual(false, operation.IsDone);
                Assert.AreEqual(1, operation.RollbackCount);
                Assert.AreEqual(0, operation.RollforwardCount);
            }
        }

        [Test]
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
            Assert.AreEqual(false, stack.IsEmpty);
            Assert.AreEqual(true, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.AreEqual(operations.Length, counter.Value);
            foreach (var operation in operations)
            {
                Assert.AreEqual(true, operation.IsDone);
                Assert.AreEqual(1, operation.RollbackCount);
                Assert.AreEqual(1, operation.RollforwardCount);
            }
        }

        [Test]
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
            Assert.AreEqual(false, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.AreEqual(5, stack.Capacity);
            Assert.AreEqual(true, stack.IsEmpty);
            Assert.AreEqual(false, stack.IsFull);
            foreach (var operation in operations)
            {
                Assert.AreEqual(true, operation.IsFrozen);
            }
        }

        [Test]
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

            Assert.AreEqual(true, operations[0].IsFrozen);
            for (var i = 1; i < operations.Length; ++i)
            {
                Assert.AreEqual(false, operations[i].IsFrozen);
                Assert.AreEqual(operations[i], ((TransactionStack)stack).Transactions[i - 1].Operations[0]);
            }
        }

        [Test]
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
                Assert.AreEqual(true, operations[0].IsFrozen);
            }
            for (var i = 3; i < operations.Length; ++i)
            {
                Assert.AreEqual(false, operations[i].IsFrozen);
                Assert.AreEqual(operations[i], ((TransactionStack)stack).Transactions[i - 3].Operations[0]);
            }
        }

        [Test]
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
                Assert.AreEqual(false, operations[i].IsFrozen);
                Assert.AreEqual(operations[i], ((TransactionStack)stack).Transactions[i].Operations[0]);
            }
            // operations[4] is the discarded transaction
            Assert.AreEqual(true, operations[4].IsFrozen);
            Assert.AreEqual(false, operations[5].IsFrozen);
            Assert.AreEqual(operations[5], ((TransactionStack)stack).Transactions[4].Operations[0]);
        }
    }
}
