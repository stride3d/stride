// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Core.Storage;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics;

public class DescriptorSetLayoutBuilder
{
    /// <summary>
    /// Helper class to build a <see cref="DescriptorSetLayout"/>.
    /// </summary>
    internal int ElementCount;

    internal List<Entry> Entries = [];

    private ObjectIdBuilder hashBuilder = new();

    public ObjectId Hash => hashBuilder.ComputeHash();


    public void AddBinding(ParameterKey key, string logicalGroup, EffectParameterClass @class, EffectParameterType type, EffectParameterType elementType, int arraySize = 1, SamplerState? immutableSampler = null)
    {
        /// <summary>
        /// Returns hash describing current state of DescriptorSet (to know if they can be shared)
        /// </summary>
        /// <summary>
        /// Gets (or creates) an entry to the DescriptorSetLayout and gets its index.
        /// </summary>
        /// <returns>The future entry index.</returns>
        hashBuilder.Write(key.Name);
        hashBuilder.Write(@class);
        hashBuilder.Write(arraySize);

        ElementCount += arraySize;
        Entries.Add(new Entry(key, logicalGroup, @class, type, elementType, arraySize, immutableSampler));
    }

    internal readonly record struct Entry
    (
        ParameterKey Key,
        string LogicalGroup,
        EffectParameterClass Class,
        EffectParameterType Type,
        EffectParameterType ElementType,
        int ArraySize,
        SamplerState? ImmutableSampler
    );
}
