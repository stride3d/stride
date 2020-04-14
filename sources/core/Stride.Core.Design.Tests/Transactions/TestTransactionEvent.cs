// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Transactions;

// ReSharper disable AccessToModifiedClosure - we use this on purpose for event testing in this file

namespace Stride.Core.Design.Tests.Transactions
{
    public class TestTransactionEvent
    {
        [Fact]
        public void TestTransactionCompleted()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            stack.TransactionCompleted += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
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
            Assert.Equal(8, expectedRaiseCount);
            Assert.Equal(8, raiseCount);
        }

        [Fact]
        public void TestEmptyTransactionCompleted()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 1;
            stack.TransactionCompleted += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                // Empty transaction
            }
            Assert.Equal(1, expectedRaiseCount);
            Assert.Equal(1, raiseCount);

            ++expectedRaiseCount;
            using (stack.CreateTransaction())
            {
                using (stack.CreateTransaction())
                {
                    // Empty transaction
                }
            }
            Assert.Equal(2, expectedRaiseCount);
            Assert.Equal(2, raiseCount);
        }

        [Fact]
        public void TestTransactionCleared()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            stack.Cleared += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
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
            Assert.Equal(1, expectedRaiseCount);
            Assert.Equal(1, raiseCount);
        }

        [Fact]
        public void TestTransactionRollbacked()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            stack.TransactionRollbacked += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
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
            Assert.Equal(stack.Capacity, expectedRaiseCount);
            Assert.Equal(stack.Capacity, raiseCount);
            Assert.Equal(5, stack.Capacity);
        }

        [Fact]
        public void TestTransactionRollforwarded()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            stack.TransactionRollforwarded += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
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
            Assert.Equal(stack.Capacity, expectedRaiseCount);
            Assert.Equal(stack.Capacity, raiseCount);
            Assert.Equal(5, stack.Capacity);
        }

        [Fact]
        public void TestTransactionDiscardStackFull()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            var transactions = new ITransaction[5];
            stack.TransactionDiscarded += (sender, e) =>
            {
                Assert.Equal(expectedRaiseCount, ++raiseCount);
                Assert.Equal(DiscardReason.StackFull, e.Reason);
                Assert.Equal(1, e.Transactions.Count);
                Assert.NotNull(e.Transactions[0]);
                Assert.Equal((object)transactions[0], e.Transactions[0]);
                Assert.True(((Operation)e.Transactions[0]).IsFrozen);
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
            stack.TransactionCompleted += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < 3; ++i)
                {
                    var operation = new SimpleOperation();
                    stack.PushOperation(operation);
                }
                expectedRaiseCount = 1;
            }
            Assert.Equal(2, expectedRaiseCount);
            Assert.Equal(2, raiseCount);
        }

        [Fact]
        public void TestTransactionDiscardPurgeLast()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            var transactions = new ITransaction[5];
            stack.TransactionDiscarded += (sender, e) =>
            {
                Assert.Equal(expectedRaiseCount, ++raiseCount);
                Assert.Equal(DiscardReason.StackPurged, e.Reason);
                Assert.Equal(1, e.Transactions.Count);
                Assert.NotNull(e.Transactions[0]);
                Assert.Equal((object)transactions[4], e.Transactions[0]);
                Assert.True(((Operation)e.Transactions[0]).IsFrozen);
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
            stack.TransactionCompleted += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < 3; ++i)
                {
                    var operation = new SimpleOperation();
                    stack.PushOperation(operation);
                    expectedRaiseCount = 1;
                }
            }
            Assert.Equal(2, expectedRaiseCount);
            Assert.Equal(2, raiseCount);
            Assert.Equal(5, stack.Capacity);
        }

        [Fact]
        public void TestTransactionDiscardPurgeMultiple()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            var transactions = new ITransaction[5];
            stack.TransactionDiscarded += (sender, e) =>
            {
                Assert.Equal(expectedRaiseCount, ++raiseCount);
                Assert.Equal(DiscardReason.StackPurged, e.Reason);
                Assert.Equal(3, e.Transactions.Count);
                for (int i = 0; i < 3; ++i)
                {
                    Assert.NotNull(e.Transactions[i]);
                    Assert.True(((Operation)e.Transactions[i]).IsFrozen);
                }
                Assert.Equal((object)transactions[2], e.Transactions[0]);
                Assert.Equal((object)transactions[3], e.Transactions[1]);
                Assert.Equal((object)transactions[4], e.Transactions[2]);
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
            stack.TransactionCompleted += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < 3; ++i)
                {
                    var operation = new SimpleOperation();
                    stack.PushOperation(operation);
                    expectedRaiseCount = 1;
                }
            }
            Assert.Equal(2, expectedRaiseCount);
            Assert.Equal(2, raiseCount);
            Assert.Equal(5, stack.Capacity);
        }

        [Fact]
        public void TestTransactionDiscardPurgeAll()
        {
            var stack = TransactionStackFactory.Create(5);
            var raiseCount = 0;
            var expectedRaiseCount = 0;
            var transactions = new ITransaction[5];
            stack.TransactionDiscarded += (sender, e) =>
            {
                Assert.Equal(expectedRaiseCount, ++raiseCount);
                Assert.Equal(DiscardReason.StackPurged, e.Reason);
                Assert.Equal(5, e.Transactions.Count);
                for (int i = 0; i < 5; ++i)
                {
                    Assert.NotNull(e.Transactions[i]);
                    Assert.Equal((object)transactions[i], e.Transactions[i]);
                    Assert.True(((Operation)e.Transactions[i]).IsFrozen);
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
            stack.TransactionCompleted += (sender, e) => Assert.Equal(expectedRaiseCount, ++raiseCount);
            using (stack.CreateTransaction())
            {
                for (var i = 0; i < 3; ++i)
                {
                    var operation = new SimpleOperation();
                    stack.PushOperation(operation);
                    expectedRaiseCount = 1;
                }
            }
            Assert.Equal(2, expectedRaiseCount);
            Assert.Equal(2, raiseCount);
            Assert.Equal(5, stack.Capacity);
        }
    }
}
