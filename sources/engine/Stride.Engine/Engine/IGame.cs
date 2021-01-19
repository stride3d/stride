using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Audio;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Input;
using Stride.Profiling;
using Stride.Rendering;
using Stride.Rendering.Fonts;
using Stride.Rendering.Sprites;
using Stride.Shaders.Compiler;
using Stride.Streaming;
using Stride.VirtualReality;

namespace Stride
{
    public interface IGame : IGameBase
    {
        // why these are static?
        ///// <summary>
        ///// Static event that will be fired when a game is initialized
        ///// </summary>
        //static event EventHandler GameStarted;

        ///// <summary>
        ///// Static event that will be fired when a game is destroyed
        ///// </summary>
        //static event EventHandler GameDestroyed;

        /// <summary>
        /// Readonly game settings as defined in the GameSettings asset
        /// Please note that it will be populated during initialization
        /// It will be ok to read them after the GameStarted event or after initialization
        /// </summary>
        GameSettings Settings { get; } // for easy transfer from PrepareContext to Initialize

        /// <summary>
        /// Gets the graphics device manager.
        /// </summary>
        /// <value>The graphics device manager.</value>
        GraphicsDeviceManager GraphicsDeviceManager { get; }

        /// <summary>
        /// Gets the script system.
        /// </summary>
        /// <value>The script.</value>
        ScriptSystem Script { get; }

        /// <summary>
        /// Gets the input manager.
        /// </summary>
        /// <value>The input.</value>
        InputManager Input { get; }

        /// <summary>
        /// Gets the scene system.
        /// </summary>
        /// <value>The scene system.</value>
        SceneSystem SceneSystem { get; }

        /// <summary>
        /// Gets the effect system.
        /// </summary>
        /// <value>The effect system.</value>
        EffectSystem EffectSystem { get; }

        /// <summary>
        /// Gets the streaming system.
        /// </summary>
        /// <value>The streaming system.</value>
        StreamingManager Streaming { get; }

        /// <summary>
        /// Gets the audio system.
        /// </summary>
        /// <value>The audio.</value>
        AudioSystem Audio { get; }

        /// <summary>
        /// Gets the sprite animation system.
        /// </summary>
        /// <value>The sprite animation system.</value>
        SpriteAnimationSystem SpriteAnimation { get; }

        /// <summary>
        /// Gets the game profiler system.
        /// </summary>
        DebugTextSystem DebugTextSystem { get; }

        /// <summary>
        /// Gets the game profiler system.
        /// </summary>
        GameProfilingSystem ProfilingSystem { get; }

        /// <summary>
        /// Gets the VR Device System.
        /// </summary>
        VRDeviceSystem VRDeviceSystem { get; }

        /// <summary>
        /// Gets the font system.
        /// </summary>
        /// <value>The font system.</value>
        /// <exception cref="System.InvalidOperationException">The font system is not initialized yet</exception>
        IFontFactory Font { get; }

        /// <summary>
        /// Gets or sets the console log mode. See remarks.
        /// </summary>
        /// <value>The console log mode.</value>
        /// <remarks>
        /// Defines how the console will be displayed when running the game. By default, on Windows, It will open only on debug
        /// if there are any messages logged.
        /// </remarks>
        ConsoleLogMode ConsoleLogMode { get; }

        /// <summary>
        /// Gets or sets the default console log level.
        /// </summary>
        /// <value>The console log level.</value>
        LogMessageType ConsoleLogLevel { get; }

        /// <summary>
        /// Automatically initializes game settings like default scene, resolution, graphics profile.
        /// </summary>
        bool AutoLoadDefaultSettings { get; set; }


    }
}
