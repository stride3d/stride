// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Various extension methods usedful to use with <see cref="LogicalGroup"/>.
    /// </summary>
    public static class LogicalGroupExtensions
    {
        /// <summary>
        /// Adds a <see cref="LogicalGroup"/> layout to a <see cref="ParameterCollectionLayout"/>, so that it is later easy to use with <see cref="UpdateLogicalGroup"/>.
        /// </summary>
        /// <param name="parameterCollectionLayout"></param>
        /// <param name="resourceGroupLayout"></param>
        /// <param name="logicalGroup"></param>
        public static void ProcessLogicalGroup(this ParameterCollectionLayout parameterCollectionLayout, RenderSystemResourceGroupLayout resourceGroupLayout, ref LogicalGroup logicalGroup)
        {
            for (int index = 0; index < logicalGroup.ConstantBufferMemberCount; ++index)
            {
                var member = resourceGroupLayout.ConstantBufferReflection.Members[logicalGroup.ConstantBufferMemberStart + index];
                parameterCollectionLayout.LayoutParameterKeyInfos.Add(new ParameterKeyInfo(member.KeyInfo.Key, parameterCollectionLayout.BufferSize + member.Offset - logicalGroup.ConstantBufferOffset, member.Type.Elements > 0 ? member.Type.Elements : 1));
            }
            for (int index = 0; index < logicalGroup.DescriptorEntryCount; ++index)
            {
                var layoutEntry = resourceGroupLayout.DescriptorSetLayoutBuilder.Entries[logicalGroup.DescriptorEntryStart + index];
                parameterCollectionLayout.LayoutParameterKeyInfos.Add(new ParameterKeyInfo(layoutEntry.Key, parameterCollectionLayout.ResourceCount++));
            }
            parameterCollectionLayout.BufferSize += logicalGroup.ConstantBufferSize;
        }

        /// <summary>
        /// Copies a full logical group of descriptors and data from a <see cref="ParameterCollection"/> to a <see cref="ResourceGroup"/>.
        /// </summary>
        /// <param name="resourceGroup">The target resource group to update.</param>
        /// <param name="logicalGroup">The logical group.</param>
        /// <param name="sourceParameters">The source values.</param>
        /// <param name="sourceDescriptorSlotStart">The source descriptor start slot (in case it contains other data before in the layout).</param>
        /// <param name="sourceOffset">The source data start offset (in case it contains other data before in the layout).</param>
        public static unsafe void UpdateLogicalGroup(this ResourceGroup resourceGroup, ref LogicalGroup logicalGroup, ParameterCollection sourceParameters, int sourceDescriptorSlotStart = 0, int sourceOffset = 0)
        {
            // Update resources
            for (int resourceSlot = 0; resourceSlot < logicalGroup.DescriptorSlotCount; ++resourceSlot)
            {
                resourceGroup.DescriptorSet.SetValue(logicalGroup.DescriptorSlotStart + resourceSlot, sourceParameters.ObjectValues[sourceDescriptorSlotStart + resourceSlot]);
            }

            // Update cbuffer
            if (logicalGroup.ConstantBufferSize > 0)
            {
                var mappedDrawLighting = resourceGroup.ConstantBuffer.Data + logicalGroup.ConstantBufferOffset;

                fixed (byte* dataValues = sourceParameters.DataValues)
                    Utilities.CopyMemory(mappedDrawLighting, (IntPtr)dataValues + sourceOffset, logicalGroup.ConstantBufferSize);
            }
        }
    }
}
