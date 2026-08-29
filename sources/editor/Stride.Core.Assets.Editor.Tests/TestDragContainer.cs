// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestDragContainer
    {
        [Fact]
        public void TestAcceptedShowsAcceptedOnly()
        {
            var container = new DragContainer(new object[] { new object() }) { Acceptance = DropAcceptance.Accepted };
            Assert.True(container.IsAccepted);
            Assert.False(container.IsRejected);
        }

        [Fact]
        public void TestRejectedShowsRejectedOnly()
        {
            var container = new DragContainer(new object[] { new object() }) { Acceptance = DropAcceptance.Rejected };
            Assert.False(container.IsAccepted);
            Assert.True(container.IsRejected);
        }

        [Fact]
        public void TestNoOpShowsNeither()
        {
            // The drag visual binds one image to each of these. A no-op must show no image at all.
            var container = new DragContainer(new object[] { new object() }) { Acceptance = DropAcceptance.NoOp };
            Assert.False(container.IsAccepted);
            Assert.False(container.IsRejected);
        }

        [Fact]
        public void TestDefaultIsRejected()
        {
            var container = new DragContainer(new object[] { new object() });
            Assert.Equal(DropAcceptance.Rejected, container.Acceptance);
        }

        [Fact]
        public void TestAcceptanceNotifiesTheDerivedProperties()
        {
            var container = new DragContainer(new object[] { new object() });
            var changed = new List<string>();
            ((INotifyPropertyChanged)container).PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            container.Acceptance = DropAcceptance.Accepted;

            Assert.Contains(nameof(DragContainer.Acceptance), changed);
            Assert.Contains(nameof(DragContainer.IsAccepted), changed);
            Assert.Contains(nameof(DragContainer.IsRejected), changed);
        }
    }
}
