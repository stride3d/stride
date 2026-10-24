// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Games;
using Stride.Input;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Xunit;

namespace Stride.UI.Tests
{
    /// <summary>
    /// Checks that the UI captures the keyboard while an element has focus, with a headless input manager and no rendering.
    /// </summary>
    public sealed class UISystemInputCaptureTests : IDisposable
    {
        private readonly InputManager input;
        private readonly KeyboardSimulated keyboard;
        private readonly UISystem ui;
        private readonly GameTime gameTime = new GameTime();

        public UISystemInputCaptureTests()
        {
            input = new InputManager();
            input.Initialize(new GameContextHeadless());
            var source = new InputSourceSimulated();
            input.Sources.Add(source);
            keyboard = source.AddKeyboard();

            var services = new ServiceRegistry();
            services.AddService(input);
            ui = new UISystem(services);
            ui.Initialize();
        }

        public void Dispose()
        {
            ui.Dispose();
            input.Dispose();
        }

        [Fact]
        public void FocusCapturesTheKeyboard()
        {
            ui.FocusedElement = new Button();

            Assert.Same(ui, keyboard.CaptureState.Owner);
            Assert.Equal(InputCapturePriority.Focus, keyboard.CaptureState.Priority);
        }

        [Fact]
        public void TypingInAFocusedElementReachesTheElementButNotTheGame()
        {
            var element = new Button();
            var received = new List<Keys>();
            element.KeyPressed += (_, e) => received.Add(e.Key);
            ui.FocusedElement = element;

            keyboard.SimulateDown(Keys.W);
            input.Update(gameTime);
            ui.Update(gameTime);

            Assert.False(input.IsKeyDown(Keys.W));
            Assert.Equal([Keys.W], received);
        }

        [Fact]
        public void ShiftSelectionWorksInAFocusedEditText()
        {
            var editText = new EditText { Text = "hello" };
            editText.Select(5, 0);
            ui.FocusedElement = editText;

            keyboard.SimulateDown(Keys.LeftShift);
            input.Update(gameTime);
            ui.Update(gameTime);
            keyboard.SimulateDown(Keys.Home);
            input.Update(gameTime);
            ui.Update(gameTime);

            Assert.Equal(0, editText.SelectionStart);
            Assert.Equal(5, editText.SelectionLength);
        }

        [Fact]
        public void HidingTheFocusedElementReturnsTheKeyboardToTheGame()
        {
            var editText = new EditText();
            var popup = new StackPanel { Children = { editText } };
            ui.FocusedElement = editText;

            popup.Visibility = Visibility.Collapsed;
            input.Update(gameTime);

            Assert.Null(ui.FocusedElement);
            Assert.False(keyboard.CaptureState.IsCaptured);
        }

        [Fact]
        public void DisablingTheFocusedElementReturnsTheKeyboardToTheGame()
        {
            var editText = new EditText();
            var popup = new StackPanel { Children = { editText } };
            ui.FocusedElement = editText;

            popup.IsEnabled = false;
            input.Update(gameTime);

            Assert.Null(ui.FocusedElement);
            Assert.False(keyboard.CaptureState.IsCaptured);
        }

        [Fact]
        public void LeavingFocusReturnsTheKeyboardToTheGame()
        {
            ui.FocusedElement = new Button();
            ui.FocusedElement = null;

            keyboard.SimulateDown(Keys.A);
            input.Update(gameTime);

            Assert.False(keyboard.CaptureState.IsCaptured);
            Assert.True(input.IsKeyPressed(Keys.A));
        }

        [Fact]
        public void FocusRecapturesTheKeyboardAfterAModalReleasesIt()
        {
            var modal = new object();
            ui.FocusedElement = new Button();
            input.TryCapture(keyboard, modal, InputCapturePriority.Modal);

            input.Release(keyboard, modal);
            input.Update(gameTime);

            Assert.Same(ui, keyboard.CaptureState.Owner);
        }

        [Fact]
        public void PickingTargetsThatAreNoLongerDrawnAreForgotten()
        {
            ui.RecordPickingTarget(new UIPickingTarget { RenderObject = new Rendering.UI.RenderUIElement(), Frame = 1 });
            ui.RecordPickingTarget(new UIPickingTarget { RenderObject = new Rendering.UI.RenderUIElement(), Frame = 2 });

            input.Update(gameTime);

            Assert.Equal(1, ui.PickingTargetCount);
        }

        [Fact]
        public void PickingTargetsAreKeptForEachViewThatDrawsAComponent()
        {
            var component = new Rendering.UI.RenderUIElement();
            ui.RecordPickingTarget(new UIPickingTarget { RenderObject = component, View = new Rendering.RenderView(), Frame = 1 });
            ui.RecordPickingTarget(new UIPickingTarget { RenderObject = component, View = new Rendering.RenderView(), Frame = 1 });

            input.Update(gameTime);

            Assert.Equal(2, ui.PickingTargetCount);
        }

        [Fact]
        public void DestroyingUISystemReleasesItsCaptures()
        {
            ui.FocusedElement = new Button();

            ui.Dispose();

            Assert.False(keyboard.CaptureState.IsCaptured);
        }
    }
}
