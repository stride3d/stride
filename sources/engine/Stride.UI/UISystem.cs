// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Controls;

namespace Stride.UI
{
    /// <summary>
    /// Interface of the UI system.
    /// </summary>
    public class UISystem : GameSystemBase, IService
    {
        internal UIBatch Batch { get; private set; }

        internal DepthStencilStateDescription KeepStencilValueState { get; private set; }

        internal DepthStencilStateDescription IncreaseStencilValueState { get; private set; }

        internal DepthStencilStateDescription DecreaseStencilValueState { get; private set; }

        private InputManager input;

        /// <summary>
        /// Represents the UI-element currently under the mouse cursor.
        /// Only elements with CanBeHitByUser == true are taken into account.
        /// Last processed element_state / ?UIComponent? with a valid element will be used.
        /// </summary>
        public UIElement UIElementUnderMouseCursor { get; internal set; }

        /// <summary>
        /// The <see cref="UIElement"/> that currently has the focus.
        /// </summary>
        public UIElement FocusedElement { get; internal set; }

        public UISystem(IServiceRegistry registry)
            : base(registry)
        {
            var gameSystems = registry.GetService<IGameSystemCollection>();
            gameSystems?.Add(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            input = Services.GetService<InputManager>();

            Enabled = true;
            Visible = false;

            if (Game != null) // thumbnail system has no game
            {
                Game.Activated += OnApplicationResumed;
                Game.Deactivated += OnApplicationPaused;
            }
        }

        protected override void Destroy()
        {
            if (Game != null) // thumbnail system has no game
            {
                Game.Activated -= OnApplicationResumed;
                Game.Deactivated -= OnApplicationPaused;
            }

            // ensure that OnApplicationPaused is called before destruction, when Game.Deactivated event is not triggered.
            OnApplicationPaused(this, EventArgs.Empty);

            base.Destroy();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            // create effect and geometric primitives
            Batch = new UIBatch(GraphicsDevice);

            // create depth stencil states
            var depthStencilDescription = new DepthStencilStateDescription(true, true)
            {
                StencilEnable = true,
                FrontFace = new DepthStencilStencilOpDescription
                {
                    StencilDepthBufferFail = StencilOperation.Keep,
                    StencilFail = StencilOperation.Keep,
                    StencilPass = StencilOperation.Keep,
                    StencilFunction = CompareFunction.Equal
                },
                BackFace = new DepthStencilStencilOpDescription
                {
                    StencilDepthBufferFail = StencilOperation.Keep,
                    StencilFail = StencilOperation.Keep,
                    StencilPass = StencilOperation.Keep,
                    StencilFunction = CompareFunction.Equal
                },
            };
            KeepStencilValueState = depthStencilDescription;

            depthStencilDescription.FrontFace.StencilPass = StencilOperation.Increment;
            depthStencilDescription.BackFace.StencilPass = StencilOperation.Increment;
            IncreaseStencilValueState = depthStencilDescription;

            depthStencilDescription.FrontFace.StencilPass = StencilOperation.Decrement;
            depthStencilDescription.BackFace.StencilPass = StencilOperation.Decrement;
            DecreaseStencilValueState = depthStencilDescription;
        }

        /// <summary>
        /// The method to call when the application is put on background.
        /// </summary>
        private void OnApplicationPaused(object sender, EventArgs e)
        {
            // validate the edit text and close the keyboard, if any edit text is currently active
            if (FocusedElement is EditText focusedEdit)
                focusedEdit.IsSelectionActive = false;
        }

        /// <summary>
        /// The method to call when the application is put on foreground.
        /// </summary>
        private void OnApplicationResumed(object sender, EventArgs e)
        {
            // revert the state of the edit text here?
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            UpdateKeyEvents();
        }

        private void UpdateKeyEvents()
        {
            if (input == null)
                return;

            if (FocusedElement == null || !FocusedElement.IsHierarchyEnabled)
                return;

            // Raise text input events
            var textEvents = input.Events.OfType<TextInputEvent>();
            bool enteredText = false;
            foreach (var textEvent in textEvents)
            {
                enteredText = true;
                FocusedElement?.RaiseTextInputEvent(new TextEventArgs
                {
                    Text = textEvent.Text,
                    Type = textEvent.Type,
                    CompositionStart = textEvent.CompositionStart,
                    CompositionLength = textEvent.CompositionLength
                });
            }

            foreach (var keyEvent in input.KeyEvents)
            {
                var key = keyEvent.Key;
                var evt = new KeyEventArgs { Key = key, Input = input };
                if (enteredText)
                    continue; // Skip key events if text was entered
                if (keyEvent.IsDown)
                {
                    FocusedElement?.RaiseKeyPressedEvent(evt);
                }
                else
                {
                    FocusedElement?.RaiseKeyReleasedEvent(evt);
                }
            }

            foreach (var key in input.DownKeys)
            {
                FocusedElement?.RaiseKeyDownEvent(new KeyEventArgs { Key = key, Input = input });
            }
        }

        public static IService NewInstance(IServiceRegistry services) => new UISystem(services);
    }
}
