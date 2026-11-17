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

        /// <summary>
        /// Tests that clearing the text of an <see cref="NumericTextBox"/> with <see cref="NumericTextBox.AllowNull"/> set
        /// commits a <see langword="null"/> value (see GitHub issue #3014).
        /// </summary>
        [Fact]
        public async Task TestClearingTextWithAllowNullCommitsNull()
        {
            double? value = null;
            var dispatcher = await WindowManagerHelper.CreateUIThread();
            try
            {
                var operation = dispatcher.InvokeAsync(() =>
                {
                    var textBox = new NumericTextBox();
                    textBox.Value = 5.0;
                    textBox.AllowNull = true;

                    textBox.Text = "";
                    textBox.Validate();

                    value = textBox.Value;
                });
                await operation.Task;
            }
            finally
            {
                WindowManagerHelper.ShutdownUIThread(dispatcher);
            }

            Assert.Null(value);
        }

        /// <summary>
        /// Tests that clearing the text of an <see cref="NumericTextBox"/> without <see cref="NumericTextBox.AllowNull"/>
        /// keeps the current value (see GitHub issue #3014).
        /// </summary>
        [Fact]
        public async Task TestClearingTextWithoutAllowNullKeepsValue()
        {
            double? value = null;
            var dispatcher = await WindowManagerHelper.CreateUIThread();
            try
            {
                var operation = dispatcher.InvokeAsync(() =>
                {
                    var textBox = new NumericTextBox();
                    textBox.Value = 5.0;

                    textBox.Text = "";
                    textBox.Validate();

                    value = textBox.Value;
                });
                await operation.Task;
            }
            finally
            {
                WindowManagerHelper.ShutdownUIThread(dispatcher);
            }

            Assert.Equal(5.0, value);
        }

        /// <summary>
        /// Tests that a whitespace-only text of an <see cref="NumericTextBox"/> with <see cref="NumericTextBox.AllowNull"/> set
        /// commits a <see langword="null"/> value (see GitHub issue #3014).
        /// </summary>
        [Fact]
        public async Task TestWhitespaceOnlyTextWithAllowNullCommitsNull()
        {
            double? value = null;
            var dispatcher = await WindowManagerHelper.CreateUIThread();
            try
            {
                var operation = dispatcher.InvokeAsync(() =>
                {
                    var textBox = new NumericTextBox();
                    textBox.Value = 5.0;
                    textBox.AllowNull = true;

                    textBox.Text = " ";
                    textBox.Validate();

                    value = textBox.Value;
                });
                await operation.Task;
            }
            finally
            {
                WindowManagerHelper.ShutdownUIThread(dispatcher);
            }

            Assert.Null(value);
        }

        /// <summary>
        /// Tests the text compatibility and coercion rules of <see cref="NumericTextBox"/> with and without
        /// <see cref="NumericTextBox.AllowNull"/> (see GitHub issue #3014).
        /// </summary>
        [Fact]
        public async Task TestTextCompatibilityAndCoercionWithAllowNull()
        {
            bool allowNullEmpty = false;
            bool notAllowNullEmpty = false;
            bool notAllowNullNumber = false;
            bool notAllowNullText = false;
            string coercedValue = null;
            var dispatcher = await WindowManagerHelper.CreateUIThread();
            try
            {
                var operation = dispatcher.InvokeAsync(() =>
                {
                    var textBox = new TestableNumericTextBox();
                    textBox.AllowNull = true;
                    allowNullEmpty = textBox.IsCompatible("");

                    textBox.AllowNull = false;
                    notAllowNullEmpty = textBox.IsCompatible("");
                    notAllowNullNumber = textBox.IsCompatible("42");
                    notAllowNullText = textBox.IsCompatible("abc");

                    textBox.Value = 5.0;
                    coercedValue = textBox.Coerce("");
                });
                await operation.Task;
            }
            finally
            {
                WindowManagerHelper.ShutdownUIThread(dispatcher);
            }

            Assert.True(allowNullEmpty);
            Assert.False(notAllowNullEmpty);
            Assert.True(notAllowNullNumber);
            Assert.False(notAllowNullText);
            Assert.Equal("5", coercedValue);
        }

        private record CapturedEvent(double? Old, double? New);

        private sealed class TestableNumericTextBox : NumericTextBox
        {
            public bool IsCompatible(string text) => IsTextCompatibleWithValueBinding(text);

            public string Coerce(string text) => CoerceTextForValidation(text);
        }
    }
}
