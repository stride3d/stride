// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Games;
using Stride.Input;

namespace Stride.Engine
{
    /// <summary>
    /// The input system updating the input manager exposed by <see cref="Game.Input"/>.
    /// </summary>
    public sealed class InputSystem : GameSystemBase
    {
        public InputSystem(IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            Manager = new InputManager().DisposeBy(this);
        }

        public InputManager Manager { get; }

        public override void Initialize()
        {
            base.Initialize();

            Manager.Initialize(Game.Context);

            Game.Activated += OnApplicationResumed;
            Game.Deactivated += OnApplicationPaused;
        }

        protected override void Destroy()
        {
            Game.Activated -= OnApplicationResumed;
            Game.Deactivated -= OnApplicationPaused;

            // ensure that OnApplicationPaused is called before destruction, when Game.Deactivated event is not triggered.
            OnApplicationPaused(this, EventArgs.Empty);

            base.Destroy();
        }

        public override void Update(GameTime gameTime) => Manager.Update(gameTime);

        private void OnApplicationPaused(object sender, EventArgs e) => Manager.Pause();

        private void OnApplicationResumed(object sender, EventArgs e) => Manager.Resume();
    }
}
