// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;
using Xenko.Core.Transactions;

namespace Xenko.Core.Design.Tests.Transactions
{
    [TestFixture]
    public class TestTransactionStack
    {
        [Test]
        public void TestConstruction()
        {
            var stack = TransactionStackFactory.Create(5);
            Assert.AreEqual(false, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.AreEqual(5, stack.Capacity);
            Assert.AreEqual(true, stack.IsEmpty);
            Assert.AreEqual(false, stack.IsFull);
        }

        [Test]
        public void TestOverCapacity()
        {
            var stack = (TransactionStack)TransactionStackFactory.Create(5);
            var operations = new SimpleOperation[6];
            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(false, stack.IsFull);
                using (stack.CreateTransaction())
                {
                    operations[i] = new SimpleOperation();
                    stack.PushOperation(operations[i]);
                }
            }
            Assert.AreEqual(true, stack.IsFull);
            Assert.AreEqual(5, stack.Capacity);
            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(operations[i], ((Transaction)stack.Transactions[i]).Operations[0]);
            }
            using (stack.CreateTransaction())
            {
                operations[5] = new SimpleOperation();
                stack.PushOperation(operations[5]);
            }
            Assert.AreEqual(5, stack.Transactions.Count);
            Assert.AreEqual(5, stack.Capacity);
            Assert.AreEqual(true, operations[0].IsFrozen);
            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(operations[i+1], ((Transaction)stack.Transactions[i]).Operations[0]);
            }
        }

        [Test]
        public void TestZeroCapacity()
        {
            var stack = (TransactionStack)TransactionStackFactory.Create(0);
            SimpleOperation operation;
            Assert.AreEqual(false, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.AreEqual(0, stack.Capacity);
            Assert.AreEqual(true, stack.IsFull);
            Assert.AreEqual(true, stack.IsEmpty);

            using (stack.CreateTransaction())
            {
                operation = new SimpleOperation();
                stack.PushOperation(operation);
            }
            Assert.AreEqual(true, operation.IsFrozen);
            using (stack.CreateTransaction())
            {
                operation = new SimpleOperation();
                stack.PushOperation(operation);
            }
            Assert.AreEqual(true, operation.IsFrozen);
            Assert.AreEqual(false, stack.CanRollback);
            Assert.AreEqual(false, stack.CanRollforward);
            Assert.AreEqual(0, stack.Capacity);
            Assert.AreEqual(true, stack.IsFull);
            Assert.AreEqual(true, stack.IsEmpty);
        }

        [Test]
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

        [Test]
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
            Assert.That(stack.TransactionInProgress);

            stack.PushOperation(operation);

            transaction2.Complete();

            // All transactions should be done now
            Assert.That(!stack.TransactionInProgress);

            // And stack has one transaction
            Assert.That(!stack.IsEmpty);
        }
    }
}
