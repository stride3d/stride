// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Shaders;

[DataContract]
public struct EffectTypeDescription
{
    /// <summary>
    /// Describes a shader parameter type.
    /// </summary>
    public EffectParameterClass Class;

    public EffectParameterType Type;

    public int RowCount;

    public int ColumnCount;

    public int Elements;

    public int ElementSize;

    public string Name;

    public EffectTypeMemberDescription[] Members;


    /// <inheritdoc/>
    public override readonly string ToString()
    {
        string name = Name is not null ? $" {Name}" : "";
        string rowsAndCols = RowCount > 1 || ColumnCount > 1 ? $" {RowCount}x{ColumnCount}" : "";
        return $"{Class}{rowsAndCols}{name}";
    }
}
