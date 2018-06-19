// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;
using Xenko.Core.Transactions;

// ReSharper disable AccessToModifiedClosure - we use this on purpose for event testing in this file

namespace Xenko.Core.Design.Tests.Transactions
{
    [TestFixture]
    public class TestTransactionEvent
    {
        [Test]
        public void TestTransactionCompleted()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            stack.TransactionCompleted += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            for (var j = 0; j < 8; ++j)
            {
                using (stack.CreateTransaction())
                {
                    for (var i = 0; i < 5; ++i)
                    {
                        var operation = new SimpleOperation();
                        stack.PushOperation(operation);
                    }
                    ++expectedRaiseCount;
                }
            }
            Assert.AreEqual(8, expectedRaiseCount);
            Assert.AreEqual(8, raiseCount);
        }

        [Test]
        public void TestEmptyTransactionCompleted()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 1;
            stack.TransactionCompleted += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                // Empty transaction
            }
            Assert.AreEqual(1, expectedRaiseCount);
            Assert.AreEqual(1, raiseCount);

            ++expectedRaiseCount;
            using (stack.CreateTransaction())
            {
                using (stack.CreateTransaction())
                {
                    // Empty transaction
                }
            }
            Assert.AreEqual(2, expectedRaiseCount);
            Assert.AreEqual(2, raiseCount);
        }

        [Test]
        public void TestTransactionCleared()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            stack.Cleared += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            for (var j = 0; j < 8; ++j)
            {
                using (stack.CreateTransaction())
                {
                    for (var i = 0; i < 5; ++i)
                    {
                        var operation = new SimpleOperation();
                        stack.PushOperation(operation);
                    }
                }
            }
            ++expectedRaiseCount;
            stack.Clear();
            Assert.AreEqual(1, expectedRaiseCount);
            Assert.AreEqual(1, raiseCount);
        }

        [Test]
        public void TestTransactionRollbacked()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            stack.TransactionRollbacked += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            for (var j = 0; j < stack.Capacity + 3; ++j)
            {
                using (stack.CreateTransaction())
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        var operation = new SimpleOperation();
                        stack.PushOperation(operation);
                    }
                }
            }
            for (var j = 0; j < stack.Capacity; ++j)
            {
                ++expectedRaiseCount;
                stack.Rollback();
            }
            Assert.AreEqual(stack.Capacity, expectedRaiseCount);
            Assert.AreEqual(stack.Capacity, raiseCount);
            Assert.AreEqual(5, stack.Capacity);
        }

        [Test]
        public void TestTransactionRollforwarded()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            stack.TransactionRollforwarded += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            for (var j = 0; j < stack.Capacity + 3; ++j)
            {
                using (stack.CreateTransaction())
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        var operation = new SimpleOperation();
                        stack.PushOperation(operation);
                    }
                }
            }
            for (var j = 0; j < stack.Capacity; ++j)
            {
                stack.Rollback();
            }
            for (var j = 0; j < stack.Capacity; ++j)
            {
                ++expectedRaiseCount;
                stack.Rollforward();
            }
            Assert.AreEqual(stack.Capacity, expectedRaiseCount);
            Assert.AreEqual(stack.Capacity, raiseCount);
            Assert.AreEqual(5, stack.Capacity);
        }

        [Test]
        public void TestTransactionDiscardStackFull()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            var transactions = new ITransaction[5];
            stack.TransactionDiscarded += (sender, e) =>
            {
                Assert.AreEqual(expectedRaiseCount, ++raiseCount);
                Assert.AreEqual(DiscardReason.StackFull, e.Reason);
                Assert.AreEqual(1, e.Transactions.Count);
                Assert.NotNull(e.Transactions[0]);
                Assert.AreEqual(transactions[0], e.Transactions[0]);
                Assert.AreEqual(true, ((Operation)e.Transactions[0]).IsFrozen);
                ++expectedRaiseCount;
            };
            for (var j = 0; j < stack.Capacity; ++j)
            {
                using (var transaction = stack.CreateTransaction())
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        var operation = new SimpleOperation();
                        stack.PushOperation(operation);
                    }
                    transactions[j] = transaction;
                }
            }
            stack.TransactionCompleted += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < 3; ++i)
                {
                    var operation = new SimpleOperation();
                    stack.PushOperation(operation);
                }
                expectedRaiseCount = 1;
            }
            Assert.AreEqual(2, expectedRaiseCount);
            Assert.AreEqual(2, raiseCount);
        }

        [Test]
        public void TestTransactionDiscardPurgeLast()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            var transactions = new ITransaction[5];
            stack.TransactionDiscarded += (sender, e) =>
            {
                Assert.AreEqual(expectedRaiseCount, ++raiseCount);
                Assert.AreEqual(DiscardReason.StackPurged, e.Reason);
                Assert.AreEqual(1, e.Transactions.Count);
                Assert.NotNull(e.Transactions[0]);
                Assert.AreEqual(transactions[4], e.Transactions[0]);
                Assert.AreEqual(true, ((Operation)e.Transactions[0]).IsFrozen);
                ++expectedRaiseCount;
            };
            for (var j = 0; j < stack.Capacity; ++j)
            {
                using (var transaction = stack.CreateTransaction())
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        var operation = new SimpleOperation();
                        stack.PushOperation(operation);
                    }
                    transactions[j] = transaction;
                }
            }
            stack.Rollback();
            stack.TransactionCompleted += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < 3; ++i)
                {
                    var operation = new SimpleOperation();
                    stack.PushOperation(operation);
                    expectedRaiseCount = 1;
                }
            }
            Assert.AreEqual(2, expectedRaiseCount);
            Assert.AreEqual(2, raiseCount);
            Assert.AreEqual(5, stack.Capacity);
        }

        [Test]
        public void TestTransactionDiscardPurgeMultiple()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            var transactions = new ITransaction[5];
            stack.TransactionDiscarded += (sender, e) =>
            {
                Assert.AreEqual(expectedRaiseCount, ++raiseCount);
                Assert.AreEqual(DiscardReason.StackPurged, e.Reason);
                Assert.AreEqual(3, e.Transactions.Count);
                for (int i = 0; i < 3; ++i)
                {
                    Assert.NotNull(e.Transactions[i]);
                    Assert.AreEqual(true, ((Operation)e.Transactions[i]).IsFrozen);
                }
                Assert.AreEqual(transactions[2], e.Transactions[0]);
                Assert.AreEqual(transactions[3], e.Transactions[1]);
                Assert.AreEqual(transactions[4], e.Transactions[2]);
                ++expectedRaiseCount;
            };
            for (var j = 0; j < stack.Capacity; ++j)
            {
                using (var transaction = stack.CreateTransaction())
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        var operation = new SimpleOperation();
                        stack.PushOperation(operation);
                    }
                    transactions[j] = transaction;
                }
            }
            stack.Rollback();
            stack.Rollback();
            stack.Rollback();
            stack.TransactionCompleted += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < 3; ++i)
                {
                    var operation = new SimpleOperation();
                    stack.PushOperation(operation);
                    expectedRaiseCount = 1;
                }
            }
            Assert.AreEqual(2, expectedRaiseCount);
            Assert.AreEqual(2, raiseCount);
            Assert.AreEqual(5, stack.Capacity);
        }

        [Test]
        public void TestTransactionDiscardPurgeAll()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            var transactions = new ITransaction[5];
            stack.TransactionDiscarded += (sender, e) =>
            {
                Assert.AreEqual(expectedRaiseCount, ++raiseCount);
                Assert.AreEqual(DiscardReason.StackPurged, e.Reason);
                Assert.AreEqual(5, e.Transactions.Count);
                for (int i = 0; i < 5; ++i)
                {
                    Assert.NotNull(e.Transactions[i]);
                    Assert.AreEqual(transactions[i], e.Transactions[i]);
                    Assert.AreEqual(true, ((Operation)e.Transactions[i]).IsFrozen);
                }
                ++expectedRaiseCount;
            };
            for (var j = 0; j < stack.Capacity; ++j)
            {
                using (var transaction = stack.CreateTransaction())
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        var operation = new SimpleOperation();
                        stack.PushOperation(operation);
                    }
                    transactions[j] = transaction;
                }
            }
            for (var j = 0; j < stack.Capacity; ++j)
            {
                stack.Rollback();
            }
            stack.TransactionCompleted += (sender, e) => Assert.AreEqual(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < 3; ++i)
                {
                    var operation = new SimpleOperation();
                    stack.PushOperation(operation);
                    expectedRaiseCount = 1;
                }
            }
            Assert.AreEqual(2, expectedRaiseCount);
            Assert.AreEqual(2, raiseCount);
            Assert.AreEqual(5, stack.Capacity);
        }
    }
}
