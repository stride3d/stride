// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Games.Time;
using Stride.Graphics;

namespace Stride.Games
{
    public interface IGame
    {
        /// <summary>
        /// Occurs when [activated].
        /// </summary>
        event EventHandler<EventArgs> Activated;

        /// <summary>
        /// Occurs when [deactivated].
        /// </summary>
        event EventHandler<EventArgs> Deactivated;

        /// <summary>
        /// Occurs when [exiting].
        /// </summary>
        event EventHandler<EventArgs> Exiting;

        /// <summary>
        /// Occurs when [window created].
        /// </summary>
        event EventHandler<EventArgs> WindowCreated;

        /// <summary>
        /// Gets the current game time.
        /// </summary>
        /// <value>The current game time.</value>
        GameTime UpdateTime { get; }

        /// <summary>
        /// Gets the current draw time.
        /// </summary>
        /// <value>The current draw time.</value>
        GameTime DrawTime { get; }

        /// <summary>
        /// Gets the draw interpolation factor, which is (<see cref="UpdateTime"/> - <see cref="DrawTime"/>) / <see cref="TargetElapsedTime"/>.
        /// If <see cref="IsFixedTimeStep"/> is false, it will be 0 as <see cref="UpdateTime"/> and <see cref="DrawTime"/> will be equal.
        /// </summary>
        /// <value>
        /// The draw interpolation factor.
        /// </value>
        float DrawInterpolationFactor { get; }

        /// <summary>
        /// Gets or sets the <see cref="ContentManager"/>.
        /// </summary>
        /// <value>The content manager.</value>
        ContentManager Content { get; }

        /// <summary>
        /// Gets the game components registered by this game.
        /// </summary>
        /// <value>The game components.</value>
        GameSystemCollection GameSystems { get; }

        /// <summary>
        /// Gets the game context.
        /// </summary>
        /// <value>The game context.</value>
        GameContext Context { get; }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        /// Gets the graphics context.
        /// </summary>
        /// <value>The graphics context.</value>
        GraphicsContext GraphicsContext { get; }

        /// <summary>
        /// Gets or sets the inactive sleep time.
        /// </summary>
        /// <value>The inactive sleep time.</value>
        TimeSpan InactiveSleepTime { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        bool IsActive { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is fixed time step.
        /// </summary>
        /// <value><c>true</c> if this instance is fixed time step; otherwise, <c>false</c>.</value>
        bool IsFixedTimeStep { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether draw can happen as fast as possible, even when <see cref="IsFixedTimeStep"/> is set.
        /// </summary>
        /// <value><c>true</c> if this instance allows desychronized drawing; otherwise, <c>false</c>.</value>
        bool IsDrawDesynchronized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mouse should be visible.
        /// </summary>
        /// <value><c>true</c> if the mouse should be visible; otherwise, <c>false</c>.</value>
        bool IsMouseVisible { get; set; }

        /// <summary>
        /// Gets the launch parameters.
        /// </summary>
        /// <value>The launch parameters.</value>
        LaunchParameters LaunchParameters { get; }

        /// <summary>
        /// Gets a value indicating whether is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the service container.
        /// </summary>
        /// <value>The service container.</value>
        ServiceRegistry Services { get; }

        /// <summary>
        /// Gets or sets the target elapsed time.
        /// </summary>
        /// <value>The target elapsed time.</value>
        TimeSpan TargetElapsedTime { get; set; }

        /// <summary>
        /// Gets the abstract window.
        /// </summary>
        /// <value>The window.</value>
        GameWindow Window { get; }
    }
}
