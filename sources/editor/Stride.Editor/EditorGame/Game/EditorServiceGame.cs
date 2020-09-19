// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Editor.Build;
using Stride.Editor.Engine;
using Stride.Games;
using Stride.Games.Time;
using Stride.Graphics;
using Stride.Rendering.Compositing;

namespace Stride.Editor.EditorGame.Game
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

        private static readonly TimeSpan GameHiddenNextUpdateTime = TimeSpan.FromSeconds(1);

        private readonly TimerTick gameHiddenUpdateTimer = new TimerTick();
        private TimeSpan gameHiddenUpdateTimeElapsed = TimeSpan.Zero;
        private bool isEditorHidden = false;
        private bool prevForceOneUpdatePerDraw;
        private bool isFirstDrawCall = true;    // We need to call BeginDraw at least once to ensure graphics context is generated

        public EditorGameServiceRegistry EditorServices { get; private set; }

        public IGameSettingsAccessor PackageSettings { get; set; }

        /// <summary>
        /// True if the game is not visible in the editor which will stop rendering and
        /// throttle game updates.
        /// </summary>
        /// <remarks>
        /// Used when game is not visible in the editor (eg. hidden inside a tab control).
        /// We only throttle updates instead of completely suspending the game because a game exit command is done
        /// within the ScriptSystem, so we must be able to run this system.
        /// </remarks>
        public bool IsEditorHidden
        {
            get { return isEditorHidden; }
            set
            {
                gameHiddenUpdateTimer.Reset();
                gameHiddenUpdateTimeElapsed = TimeSpan.Zero;
                isEditorHidden = value;
                if (isEditorHidden)
                {
                    // In case the editor is set to IsFixedTimeStep, we need to ensure when the game
                    // updates while we're throttling it only updates once instead of trying to 'catch up' on update calls.
                    prevForceOneUpdatePerDraw = ForceOneUpdatePerDraw;
                    ForceOneUpdatePerDraw = true;
                }
                else
                {
                    ForceOneUpdatePerDraw = prevForceOneUpdatePerDraw;  // Restore the previous setting
                }
            }
        }

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

        protected override void PrepareContext()
        {
            Services.RemoveService<IDatabaseFileProviderService>();
            Services.AddService(MicrothreadLocalDatabases.ProviderService);

            base.PrepareContext();
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            // Database is needed by effect compiler cache
            MicrothreadLocalDatabases.MountCommonDatabase();

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

            if (IsEditorHidden)
            {
                gameHiddenUpdateTimer.Tick();
                gameHiddenUpdateTimeElapsed += gameHiddenUpdateTimer.ElapsedTime;
                if (gameHiddenUpdateTimeElapsed < GameHiddenNextUpdateTime)
                {
                    return;
                }
                else
                {
                    gameHiddenUpdateTimeElapsed = TimeSpan.Zero;    // It doesn't matter how much it exceeded the threshold, taking longer than one second is ok since it's hidden
                }
            }

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

        protected override bool BeginDraw()
        {
            if (IsEditorHidden && !isFirstDrawCall)
            {
                // While it's hidden do not prepare the graphics context
                return false;
            }
            isFirstDrawCall = false;
            return base.BeginDraw();
        }

        /// <inheritdoc />
        protected override void Draw(GameTime gameTime)
        {
            // Keep going only if last exception has been "resolved"
            if (Faulted || IsEditorHidden)
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
