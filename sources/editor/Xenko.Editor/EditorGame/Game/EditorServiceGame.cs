// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Editor.Build;
using Xenko.Editor.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Rendering.Compositing;

namespace Xenko.Editor.EditorGame.Game
{
    /// <summary>
    /// Represents the arguments of the <see cref="EditorServiceGame.ExceptionThrown"/> event.
    /// </summary>
    public class ExceptionThrownEventArgs : EventArgs
    {
        public ExceptionThrownEventArgs([NotNull] Exception exception)
        {
            Exception = exception;
        }

        /// <summary>
        /// The exception that was thrown.
        /// </summary>
        [NotNull]
        public Exception Exception { get; }

        /// <summary>
        /// Gets or sets a value that indicates the present state of the event handling.
        /// </summary>
        /// <remarks>
        /// If <c>true</c> the game will go to the faulted state; otherwise, the exception will be rethrown.
        /// </remarks>
        public bool Handled { get; set; }
    }

    public abstract class EditorServiceGame : EmbeddedGame
    {
        public static readonly Color EditorBackgroundColorLdr = new Color(51, 51, 51, 255);

        public static readonly Color EditorBackgroundColorHdr = new Color(61, 61, 61, 255);

        public EditorGameServiceRegistry EditorServices { get; private set; }

        public IGameSettingsAccessor PackageSettings { get; set; }

        /// <summary>
        /// True if game is faulted (not running).
        /// </summary>
        /// <remarks>
        /// Game won't resume until cleared.
        /// </remarks>
        public bool Faulted { get; set; }

        public event EventHandler<ExceptionThrownEventArgs> ExceptionThrown;

        /// <summary>
        /// Calculates and returns the position of the mouse in the scene.
        /// </summary>
        /// <param name="mousePosition">The position of the mouse.</param>
        /// <returns>The position in the scene world space.</returns>
        public abstract Vector3 GetPositionInScene(Vector2 mousePosition);

        public void RegisterServices(EditorGameServiceRegistry serviceRegistry)
        {
            EditorServices = serviceRegistry;
        }

        public abstract void TriggerActiveRenderStageReevaluation();

        public void UpdateColorSpace(ColorSpace colorSpace)
        {
            // Change the color space if necessary
            if (GraphicsDeviceManager.PreferredColorSpace != colorSpace)
            {
                GraphicsDeviceManager.PreferredColorSpace = colorSpace;
                GraphicsDeviceManager.ApplyChanges();
            }
        }

        public virtual void UpdateGraphicsCompositor(GraphicsCompositor graphicsCompositor)
        {
            SceneSystem.GraphicsCompositor = graphicsCompositor;
            SceneSystem.GraphicsCompositor.Game = new EditorTopLevelCompositor { Child = SceneSystem.GraphicsCompositor.Editor, PreviewGame = SceneSystem.GraphicsCompositor.Game };

            foreach (var service in EditorServices.Services)
            {
                service.UpdateGraphicsCompositor(this);
            }
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            // TODO: the physics system should not be registered by default here!
            Physics.Simulation.DisableSimulation = true;
        }

        // <inheritdoc />
        protected override void Update(GameTime gameTime)
        {
            // Keep going only if last exception has been "resolved"
            if (Faulted)
                return;

            try
            {
                base.Update(gameTime);
            }
            catch (Exception ex)
            {
                if (!OnFault(ex))
                {
                    // Exception was no handled, rethrow
                    throw;
                }
                // Caught exception, turning game into faulted state
                Faulted = true;
            }
        }

        /// <inheritdoc />
        protected override void Draw(GameTime gameTime)
        {
            // Keep going only if last exception has been "resolved"
            if (Faulted)
                return;

            try
            {
                base.Draw(gameTime);
            }
            catch (Exception ex)
            {
                if (!OnFault(ex))
                {
                    // Exception was no handled, rethrow
                    throw;
                }
                // Caught exception, turning game into faulted state
                Faulted = true;
            }
        }

        /// <summary>
        /// Called whenever an exception occured in the game.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <returns>
        /// <c>true</c> if the exception was handled and the game should transitioned to the faulted state; otherwise, <c>false</c> and the exception will be rethrown.
        /// </returns>
        /// <remarks>
        /// The exception can be handled by listener to the <see cref="ExceptionThrown"/> event.
        /// </remarks>
        protected virtual bool OnFault(Exception ex)
        {
            var handler = ExceptionThrown;
            if (handler == null)
            {
                return false;
            }
            var args = new ExceptionThrownEventArgs(ex);
            ExceptionThrown?.Invoke(this, args);
            return args.Handled;
        }
    }
}
