// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Storage;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics
{
    /// <summary>
    /// Helper class to build a <see cref="DescriptorSetLayout"/>.
    /// </summary>
    public class DescriptorSetLayoutBuilder
    {
        internal int ElementCount;
        internal List<Entry> Entries = new List<Entry>();

        private ObjectIdBuilder hashBuilder = new ObjectIdBuilder();

        /// <summary>
        /// Returns hash describing current state of DescriptorSet (to know if they can be shared)
        /// </summary>
        public ObjectId Hash => hashBuilder.ComputeHash();

        /// <summary>
        /// Gets (or creates) an entry to the DescriptorSetLayout and gets its index.
        /// </summary>
        /// <returns>The future entry index.</returns>
        public void AddBinding(ParameterKey key, string logicalGroup, EffectParameterClass @class, EffectParameterType type, EffectParameterType elementType, int arraySize = 1, SamplerState immutableSampler = null)
        {
            hashBuilder.Write(key.Name);
            hashBuilder.Write(@class);
            hashBuilder.Write(arraySize);

            ElementCount += arraySize;
            Entries.Add(new Entry { Key = key, LogicalGroup = logicalGroup, Class = @class, Type = type, ElementType = elementType, ArraySize = arraySize, ImmutableSampler = immutableSampler });
        }

        internal struct Entry
        {
            public ParameterKey Key;
            public string LogicalGroup;
            public EffectParameterClass Class;
            public EffectParameterType Type;
            public EffectParameterType ElementType;
            public int ArraySize;
            public SamplerState ImmutableSampler;
        }
    }
}
