// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Graphics
{
    /// <summary>
    /// Used internally to store descriptor entries.
    /// </summary>
    internal struct DescriptorSetEntry
    {
        public object Value;

        //used only for compute shader
        public int UAVInitialOffset;

        public DescriptorSetEntry(object value, int uavInitialOffset = -1)
        {
            Value = value;
            UAVInitialOffset = uavInitialOffset;
        }
    }
}
