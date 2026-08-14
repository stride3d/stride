// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Data;
using Stride.Graphics;

namespace Stride.Engine.Design
{
    /// <summary>
    /// Stores some default parameters for the game.
    /// </summary>
    [DataContract("GameSettings")]
    [ContentSerializer(typeof(DataContentSerializer<GameSettings>))]
    public sealed class GameSettings
    {
        public const string AssetUrl = "GameSettings";

        public GameSettings()
        {
            EffectCompilation = EffectCompilationMode.Local;
        }

        public string PackageName { get; set; }

        public string DefaultSceneUrl { get; set; }

        public string DefaultGraphicsCompositorUrl { get; set; }

        public string SplashScreenUrl { get; set; }

        public Color4 SplashScreenColor { get; set; }

        public bool DoubleViewSplashScreen { get; set; }

        /// <summary>
        /// Gets or sets the compilation mode used.
        /// </summary>
        public CompilationMode CompilationMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether effect compile should be allowed, and if yes, should it be done locally (if possible) or remotely?
        /// </summary>
        public EffectCompilationMode EffectCompilation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether effect compile (local or remote) should be recorded and sent to effect compile server for GameStudio notification.
        /// </summary>
        public bool RecordUsedEffects { get; set; }

        /// <summary>
        /// Gets or sets configuration for the actual running platform as compiled during build
        /// </summary>
        public List<Configuration> Configurations { get; set; } = [];

        /// <summary>Retrieves a configuration of a given type.</summary>
        /// <typeparam name="T">The type of the configuration to retrieve.</typeparam>
        /// <returns>Returns the configuration.</returns>
        /// <remarks>If <see cref="Configurations"/> doesn't contain the configuration, the method will return a newly created instance and add it to the list, so that future calls will keep returning the same instance.</remarks>
        public T GetOrCreateConfiguration<T>() where T : Configuration, new()
        {
            var config = Configurations.OfType<T>().FirstOrDefault();
            if (config is null)
            {
                config = new T();
                Configurations.Add(config);
            }

            return config;
        }
    }
}
