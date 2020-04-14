// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Defines the context used by a stage for a layer.
    /// </summary>
    internal class MaterialBlendLayerPerStageContext
    {
        public MaterialBlendLayerPerStageContext()
        {
            ShaderSources = new List<ShaderSource>();
            StreamInitializers = new List<string>();
            Streams = new HashSet<string>();
        }

        public List<ShaderSource> ShaderSources { get; }

        public List<string> StreamInitializers { get; }

        public HashSet<string> Streams { get; }

        public void Reset()
        {
            ShaderSources.Clear();
            StreamInitializers.Clear();
            Streams.Clear();
        }

        /// <summary>
        /// Squash <see cref="ShaderSources"/> to a single ShaderSource (compatible with IComputeColor)
        /// </summary>
        /// <returns>The squashed <see cref="ShaderSource"/> or null if nothing to squash</returns>
        public ShaderSource ComputeShaderSource()
        {
            if (ShaderSources.Count == 0)
            {
                return null;
            }

            ShaderSource result;
            // If there is only a single op, don't generate a mixin
            if (ShaderSources.Count == 1)
            {
                result = ShaderSources[0];
            }
            else
            {
                var mixin = new ShaderMixinSource();
                result = mixin;
                mixin.Mixins.Add(new ShaderClassSource("MaterialSurfaceArray"));

                // Squash all operations into MaterialLayerArray
                foreach (var operation in ShaderSources)
                {
                    mixin.AddCompositionToArray("layers", operation);
                }
            }
            return result;
        }
    }
}
