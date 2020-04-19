// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Shaders;

namespace Stride.Graphics
{
    // D3D11 implementation
    /// <summary>
    /// Used internally to store descriptor layout entries.
    /// </summary>
    internal struct DescriptorSetLayoutEntry
    {
        public EffectParameterClass Type;
        public int ArraySize;

        public DescriptorSetLayoutEntry(EffectParameterClass type, int arraySize = 1) : this()
        {
            Type = type;
            ArraySize = arraySize;
        }
    }
}
