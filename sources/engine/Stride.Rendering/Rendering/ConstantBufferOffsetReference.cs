// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Rendering
{
    /// <summary>
    /// Handle used to query what's the actual offset of a given variable in a constant buffer, through <see cref="ResourceGroupLayout.GetConstantBufferOffset"/>.
    /// </summary>
    public struct ConstantBufferOffsetReference
    {
        public static readonly ConstantBufferOffsetReference Invalid = new ConstantBufferOffsetReference(-1);

        internal int Index;

        internal ConstantBufferOffsetReference(int index)
        {
            Index = index;
        }
    }
}
