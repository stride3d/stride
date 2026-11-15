// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Presentation.Controls;

namespace Stride.Core.Presentation.Tests
{
    /// <summary>
    /// Tests the <see cref="NumericTextBox.ValueChanged"/> routed event (see GitHub issue #3488).
    /// The event must be raisable and subscribable with the nullable <see cref="NumericTextBox.Value"/> payload.
    /// </summary>
    public class TestNumericTextBox
    {
        [Fact]
        public async Task TestValueChangedEvent()
        {
            var dispatcher = await WindowManagerHelper.CreateUIThread();
            List<CapturedEvent> capturedEvents = new List<CapturedEvent>();
            try
            {
                var operation = dispatcher.InvokeAsync(() =>
                {
                    var textBox = new NumericTextBox();
                    textBox.ValueChanged += (_, e) => capturedEvents.Add(new CapturedEvent(e.OldValue, e.NewValue));

                    textBox.Value = 15.0;
                    textBox.Value = 30.0;
                    textBox.Value = null;
                });
                await operation.Task;
            }
            finally
            {
                WindowManagerHelper.ShutdownUIThread(dispatcher);
            }

            Assert.NotNull(capturedEvents);
            Assert.Equal(3, capturedEvents.Count);

            Assert.Equal(0.0, capturedEvents[0].Old);
            Assert.Equal(15.0, capturedEvents[0].New);

            Assert.Equal(15.0, capturedEvents[1].Old);
            Assert.Equal(30.0, capturedEvents[1].New);

            Assert.Equal(30.0, capturedEvents[2].Old);
            Assert.Null(capturedEvents[2].New);
        }

        private record CapturedEvent(double? Old, double? New);
    }
}
