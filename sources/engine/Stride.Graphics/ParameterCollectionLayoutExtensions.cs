// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics;

/// <summary>
///   Provides extension methods for updating <see cref="ParameterCollectionLayout"/> instances.
/// </summary>
public static class ParameterCollectionLayoutExtensions
{
    /// <summary>
    ///   Updates a parameter collection layout with information from a Descriptor Set layout.
    /// </summary>
    /// <param name="parameterCollectionLayout">
    ///   The <see cref="ParameterCollectionLayout"/> to be updated with resource information.
    /// </param>
    /// <param name="layout">
    ///   The <see cref="DescriptorSetLayoutBuilder"/> containing the resource entries to process.
    /// </param>
    /// <remarks>
    ///   This method iterates over the entries in the provided <paramref name="layout"/> and
    ///   adds corresponding <see cref="ParameterKeyInfo"/> objects to the
    ///   <paramref name="parameterCollectionLayout"/>.
    ///   The resource count in <paramref name="parameterCollectionLayout"/> is incremented
    ///   for each entry processed.
    /// </remarks>
    public static void ProcessResources(this ParameterCollectionLayout parameterCollectionLayout, DescriptorSetLayoutBuilder layout)
    {
        foreach (var layoutEntry in layout.Entries)
        {
            var parameterKeyInfo = new ParameterKeyInfo(layoutEntry.Key, parameterCollectionLayout.ResourceCount);
            parameterCollectionLayout.ResourceCount++;

            parameterCollectionLayout.LayoutParameterKeyInfos.Add(parameterKeyInfo);
        }
    }

    /// <summary>
    ///   Updates a parameter collection layout with a description of an Effect's Constant Buffer.
    /// </summary>
    /// <param name="parameterCollectionLayout">
    ///   The <see cref="ParameterCollectionLayout"/> to be updated with resource information.
    /// </param>
    /// <param name="constantBuffer">
    ///   The description of an Effect's Constant Buffer containing members to process.
    /// </param>
    /// <remarks>
    ///   This method iterates over the members of the provided Constant Buffer,
    ///   creating and adding <see cref="ParameterKeyInfo"/> instances to the layout.
    ///   It also updates the buffer size of the parameter collection layout by adding
    ///   the size of the Constant Buffer.
    /// </remarks>
    public static void ProcessConstantBuffer(this ParameterCollectionLayout parameterCollectionLayout, EffectConstantBufferDescription constantBuffer)
    {
        foreach (var member in constantBuffer.Members)
        {
            var parameterKeyInfo = new ParameterKeyInfo(
                member.KeyInfo.Key,
                parameterCollectionLayout.BufferSize + member.Offset,
                member.Type.Elements > 0 ? member.Type.Elements : 1);

            parameterCollectionLayout.LayoutParameterKeyInfos.Add(parameterKeyInfo);
        }
        parameterCollectionLayout.BufferSize += constantBuffer.Size;
    }
}
