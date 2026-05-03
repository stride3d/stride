// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Core.Storage;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics;

/// <summary>
///   Helper class to build a <see cref="DescriptorSetLayout"/>.
/// </summary>
public class DescriptorSetLayoutBuilder
{
    /// <summary>
    ///   The total number of elements in the DescriptorSetLayout.
    /// </summary>
    internal int ElementCount;

    /// <summary>
    ///   A list of entries that define the layout of a Descriptor Set.
    /// </summary>
    internal List<Entry> Entries = [];

    private ObjectIdBuilder hashBuilder = new();

    /// <summary>
    ///   Gets a hash identifying the current state of a Descriptor Set.
    ///   This hash is used to know if the Descriptors in the set can be shared.
    /// </summary>
    public ObjectId Hash => hashBuilder.ComputeHash();


    /// <summary>
    ///   Adds a binding to the Descriptor Set layout.
    /// </summary>
    /// <param name="key">The <see cref="ParameterKey"/> by which the Graphics Resource' Descriptor can be identified in the Shader.</param>
    /// <param name="logicalGroup">A logical group name, used to group related Descriptors and variables together.</param>
    /// <param name="class">The kind of the parameter in the Shader.</param>
    /// <param name="type">The data type of the parameter in the Shader.</param>
    /// <param name="elementType">
    ///   The data type of the elements of the parameter in the Shader in case <paramref name="Type"/> represents an
    ///   <strong>Array</strong>, <strong>Buffer</strong>, or <strong>Texture</strong> type.
    /// </param>
    /// <param name="arraySize">
    ///   The number of elements in the Array in case <paramref name="Type"/> represents an <strong>Array type</strong>.
    ///   Specify <c>1</c> if the parameter is not an Array. This is the default value.
    /// </param>
    /// <param name="immutableSampler">
    ///   An optional unmodifiable Sampler State described directly in the Shader for sampling the parameter.
    ///   Specify <see langword="null"/> if the parameter does not require a Sampler State.
    /// </param>
    public void AddBinding(ParameterKey key, string logicalGroup, EffectParameterClass @class, EffectParameterType type, EffectParameterType elementType, int arraySize = 1, SamplerState? immutableSampler = null)
    {
        hashBuilder.Write(key.Name);
        hashBuilder.Write(@class);
        hashBuilder.Write(arraySize);

        ElementCount += arraySize;
        Entries.Add(new Entry(key, logicalGroup, @class, type, elementType, arraySize, immutableSampler));
    }

    /// <summary>
    ///   Represents an resource bindings in a Descriptor Set layout, containing metadata about a parameter's key, grouping,
    ///   type, and other attributes.
    /// </summary>
    /// <param name="Key">The <see cref="ParameterKey"/> by which the Graphics Resource' Descriptor can be identified in the Shader.</param>
    /// <param name="LogicalGroup">A logical group name, used to group related Descriptors and variables together.</param>
    /// <param name="Class">The kind of the parameter in the Shader.</param>
    /// <param name="Type">The data type of the parameter in the Shader.</param>
    /// <param name="ElementType">
    ///   The data type of the elements of the parameter in the Shader in case <paramref name="Type"/> represents an
    ///   <strong>Array</strong>, <strong>Buffer</strong>, or <strong>Texture</strong> type.
    /// </param>
    /// <param name="ArraySize">
    ///   The number of elements in the Array in case <paramref name="Type"/> represents an <strong>Array type</strong>.
    ///   Specify <c>1</c> if the parameter is not an Array.
    /// </param>
    /// <param name="ImmutableSampler">
    ///   An optional unmodifiable Sampler State described directly in the Shader for sampling the parameter.
    ///   Specify <see langword="null"/> if the parameter does not require a Sampler State.
    /// </param>
    internal readonly record struct Entry
    (
        ParameterKey Key,
        string LogicalGroup,
        EffectParameterClass Class,
        EffectParameterType Type,
        EffectParameterType ElementType,
        int ArraySize,
        SamplerState? ImmutableSampler
    )
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            string logicalGroup = string.IsNullOrEmpty(LogicalGroup) ? string.Empty : $"LogicalGroup = {LogicalGroup}, ";
            string elementType = ElementType == EffectParameterType.Void ? string.Empty : ", ElementType = " + ElementType;
            string arraySize = ArraySize == 1 ? string.Empty : ", ArraySize = " + ArraySize;
            string sampler = ImmutableSampler is null ? string.Empty : ", ImmutableSampler = " + ImmutableSampler;

            return $"{Key} ({logicalGroup}Class = {Class}, Type = {Type}{elementType}{arraySize}{sampler})";
        }
    }
}
