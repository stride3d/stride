// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Presentation.Tests.Dirtiables
{
    public class TestDirtiable
    {
        [Fact]
        public void TestDoAction()
        {
            var stack = new TransactionStack(5);
            using (new DirtiableManager(stack))
            {
                var dirtiable = new SimpleDirtiable();
                using (stack.CreateTransaction())
                {
                    var operation = new SimpleDirtyingOperation(dirtiable.Yield());
                    stack.PushOperation(operation);
                }
                Assert.True(dirtiable.IsDirty);
            }
        }

        [Fact]
        public void TestDoAndSave()
        {
            var stack = new TransactionStack(5);
            using (var manager = new DirtiableManager(stack))
            {
                var dirtiable = new SimpleDirtiable();
                var operation = new SimpleDirtyingOperation(dirtiable.Yield());
                using (stack.CreateTransaction())
                {
                    stack.PushOperation(operation);
                }
                Assert.True(dirtiable.IsDirty);
                manager.CreateSnapshot();
                Assert.False(dirtiable.IsDirty);
            }
        }

        [Fact]
        public void TestUndo()
        {
            var stack = new TransactionStack(5);
            using (new DirtiableManager(stack))
            {
                var dirtiable = new SimpleDirtiable();
                var operation = new SimpleDirtyingOperation(dirtiable.Yield());
                using (stack.CreateTransaction())
                {
                    stack.PushOperation(operation);
                }
                Assert.True(dirtiable.IsDirty);
                stack.Rollback();
                Assert.False(dirtiable.IsDirty);
            }
        }

        [Fact]
        public void TestRedo()
        {
            var stack = new TransactionStack(5);
            using (new DirtiableManager(stack))
            {
                var dirtiable = new SimpleDirtiable();
                var operation = new SimpleDirtyingOperation(dirtiable.Yield());
                using (stack.CreateTransaction())
                {
                    stack.PushOperation(operation);
                }
                Assert.True(dirtiable.IsDirty);
                stack.Rollback();
                stack.Rollforward();
                Assert.True(dirtiable.IsDirty);
            }
        }

        [Fact]
        public void TestSaveUndoSaveRedo()
        {
            var stack = new TransactionStack(5);
            using (var manager = new DirtiableManager(stack))
            {
                var dirtiable = new SimpleDirtiable();
                var operation = new SimpleDirtyingOperation(dirtiable.Yield());
                using (stack.CreateTransaction())
                {
                    stack.PushOperation(operation);
                }
                Assert.True(dirtiable.IsDirty);
                manager.CreateSnapshot();
                Assert.False(dirtiable.IsDirty);
                stack.Rollback();
                Assert.True(dirtiable.IsDirty);
                manager.CreateSnapshot();
                Assert.False(dirtiable.IsDirty);
                stack.Rollforward();
                Assert.True(dirtiable.IsDirty);
                manager.CreateSnapshot();
                Assert.False(dirtiable.IsDirty);
            }
        }
    }
}
