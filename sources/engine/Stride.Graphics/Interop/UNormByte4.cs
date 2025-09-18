// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;

namespace Stride.Graphics.Interop;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct UNormByte4(byte x, byte y, byte z, byte w)
{
    public byte X = x, Y = y, Z = z, W = w;
}
