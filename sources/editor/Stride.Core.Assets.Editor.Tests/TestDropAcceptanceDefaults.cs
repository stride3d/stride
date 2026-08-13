// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xunit;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Tests
{
    /// <summary>
    /// Verifies that a view model which implements only the boolean members keeps its behaviour.
    /// </summary>
    public class TestDropAcceptanceDefaults
    {
        private const string ExpectedMessage = "message from the view model";

        private class LegacyAddChild : IAddChildViewModel
        {
            private readonly bool canAdd;

            public LegacyAddChild(bool canAdd) { this.canAdd = canAdd; }

            public bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
            {
                message = ExpectedMessage;
                return canAdd;
            }

            public void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers) { }
        }

        private class LegacyInsertChild : IInsertChildViewModel
        {
            private readonly bool canInsert;

            public LegacyInsertChild(bool canInsert) { this.canInsert = canInsert; }

            public bool CanInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
            {
                message = ExpectedMessage;
                return canInsert;
            }

            public void InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers) { }
        }

        [Theory]
        [InlineData(true, DropAcceptance.Accepted)]
        [InlineData(false, DropAcceptance.Rejected)]
        public void TestAddChildrenDefaultFollowsTheBoolean(bool canAdd, DropAcceptance expected)
        {
            IAddChildViewModel viewModel = new LegacyAddChild(canAdd);

            var acceptance = viewModel.GetAddChildrenAcceptance(new object[0], AddChildModifiers.None, out var message);

            Assert.Equal(expected, acceptance);
            Assert.Equal(ExpectedMessage, message);
        }

        [Theory]
        [InlineData(true, DropAcceptance.Accepted)]
        [InlineData(false, DropAcceptance.Rejected)]
        public void TestInsertChildrenDefaultFollowsTheBoolean(bool canInsert, DropAcceptance expected)
        {
            IInsertChildViewModel viewModel = new LegacyInsertChild(canInsert);

            var acceptance = viewModel.GetInsertChildrenAcceptance(new object[0], InsertPosition.Before, AddChildModifiers.None, out var message);

            Assert.Equal(expected, acceptance);
            Assert.Equal(ExpectedMessage, message);
        }
    }
}
