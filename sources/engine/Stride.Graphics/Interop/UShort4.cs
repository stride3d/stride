// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;

namespace Stride.Graphics.Interop;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct UShort4(ushort x, ushort y, ushort z, ushort w)
{
    public ushort X = x, Y = y, Z = z, W = w;
}
