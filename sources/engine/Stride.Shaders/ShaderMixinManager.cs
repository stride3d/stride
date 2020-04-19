// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Stride.Rendering;

namespace Stride.Shaders
{
    /// <summary>
    /// Manages <see cref="IShaderMixinBuilder"/> and generation of shader mixins.
    /// </summary>
    public class ShaderMixinManager
    {
        private static readonly Dictionary<string, IShaderMixinBuilder> RegisteredBuilders = new Dictionary<string, IShaderMixinBuilder>();

        /// <summary>
        /// Registers a <see cref="IShaderMixinBuilder"/> with the specified sdfx effect name.
        /// </summary>
        /// <param name="sdfxEffectName">Name of the mixin.</param>
        /// <param name="builder">The builder.</param>
        /// <exception cref="System.ArgumentNullException">
        /// sdfxEffectName
        /// or
        /// builder
        /// </exception>
        public static void Register(string sdfxEffectName, IShaderMixinBuilder builder)
        {
            if (sdfxEffectName == null)
                throw new ArgumentNullException("sdfxEffectName");

            if (builder == null)
                throw new ArgumentNullException("builder");

            lock (RegisteredBuilders)
            {
                RegisteredBuilders[sdfxEffectName] = builder;
            }
        }

        /// <summary>
        /// Determines whether the specified PDXFX effect is registered.
        /// </summary>
        /// <param name="sdfxEffectName">Name of the PDXFX effect.</param>
        /// <returns><c>true</c> if the specified PDXFX effect is registered; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">sdfxEffectName</exception>
        public static bool Contains(string sdfxEffectName)
        {
            if (sdfxEffectName == null) throw new ArgumentNullException("sdfxEffectName");

            var effectName = GetEffectName(sdfxEffectName);
            var rootEffectName = effectName.Key;

            lock (RegisteredBuilders)
            {
                return RegisteredBuilders.ContainsKey(rootEffectName);
            }
        }

        /// <summary>
        /// Tries to get a <see cref="IShaderMixinBuilder"/> by its name.
        /// </summary>
        /// <param name="sdfxEffectName">Name of the mixin.</param>
        /// <param name="builder">The builder instance found or null if not found.</param>
        /// <returns><c>true</c> if the builder was found, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">sdfxEffectName</exception>
        public static bool TryGet(string sdfxEffectName, out IShaderMixinBuilder builder)
        {
            if (sdfxEffectName == null)
                throw new ArgumentNullException("sdfxEffectName");

            lock (RegisteredBuilders)
            {
                return RegisteredBuilders.TryGetValue(sdfxEffectName, out builder);
            }
        }

        /// <summary>
        /// Generates a <see cref="ShaderMixinSource" /> for the specified names and parameters.
        /// </summary>
        /// <param name="sdfxEffectName">The name.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>The result of the mixin.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// sdfxEffectName
        /// or
        /// properties
        /// </exception>
        /// <exception cref="System.ArgumentException">sdfxEffectName</exception>
        public static ShaderMixinSource Generate(string sdfxEffectName, ParameterCollection properties)
        {
            if (sdfxEffectName == null) throw new ArgumentNullException("sdfxEffectName");

            if (properties == null)
                throw new ArgumentNullException("properties");

            // Get the effect name and child effect name "RootEffectName.ChildEffectName"
            var effectName = GetEffectName(sdfxEffectName);
            var rootEffectName = effectName.Key;
            var childEffectName = effectName.Value;

            IShaderMixinBuilder builder;
            Dictionary<string, IShaderMixinBuilder> builders;
            lock (RegisteredBuilders)
            {
                if (!TryGet(rootEffectName, out builder))
                    throw new ArgumentException(string.Format("Xkfx effect [{0}] not found", rootEffectName), "sdfxEffectName");

                builders = new Dictionary<string, IShaderMixinBuilder>(RegisteredBuilders);
            }

            // TODO cache mixin context and avoid to recreate one (check if if thread concurrency could occur here)
            var mixinTree = new ShaderMixinSource() { Name = sdfxEffectName };
            var context = new ShaderMixinContext(mixinTree, properties, builders) { ChildEffectName = childEffectName };
            try
            {
                builder.Generate(mixinTree, context);
            }
            catch (ShaderMixinDiscardException)
            {
                // We don't rethrow as this exception is on purpose to early exit/escape from a shader mixin
            }
            return mixinTree;
        }

        private static KeyValuePair<string, string> GetEffectName(string sdfxEffectName)
        {
            var mainEffectNameEnd = sdfxEffectName.IndexOf('.');
            var rootEffectName = mainEffectNameEnd != -1 ? sdfxEffectName.Substring(0, mainEffectNameEnd) : sdfxEffectName;
            var childEffectName = mainEffectNameEnd != -1 ? sdfxEffectName.Substring(mainEffectNameEnd + 1) : string.Empty;
            return new KeyValuePair<string, string>(rootEffectName, childEffectName);
        }

        /// <summary>
        /// Un-register all registered <see cref="IShaderMixinBuilder"/>.
        /// </summary>
        public static void UnRegisterAll()
        {
            lock (RegisteredBuilders)
            {
                RegisteredBuilders.Clear();
            }
        }
    }
}
