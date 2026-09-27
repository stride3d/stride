// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Games;
using Stride.Input;
using Stride.UI.Controls;
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
        public void DestroyingUISystemReleasesItsCaptures()
        {
            ui.FocusedElement = new Button();

            ui.Dispose();

            Assert.False(keyboard.CaptureState.IsCaptured);
        }
    }
}
