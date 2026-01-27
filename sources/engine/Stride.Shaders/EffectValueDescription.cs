// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

using Stride.Core;

namespace Stride.Shaders;

[DataContract]
[DebuggerDisplay("{Type.Class}{Type.RowCount}x{Type.ColumnCount} {KeyInfo.KeyName} -> {RawName}")]
public struct EffectValueDescription
{
    /// <summary>
    /// Describes a shader parameter for a valuetype (usually stored in constant buffers).
    /// </summary>
    public EffectTypeDescription Type;

    public EffectParameterKeyInfo KeyInfo;

    public string RawName;

    public int Offset;

    public int Size;

    public object DefaultValue;

    public string LogicalGroup;
}
