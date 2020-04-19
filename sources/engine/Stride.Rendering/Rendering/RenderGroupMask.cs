// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Rendering
{
    /// <summary>
    /// A mask for <see cref="RenderGroup"/>.
    /// </summary>
    [Flags]
    [DataContract]
    public enum RenderGroupMask : uint
    {
        None = 0,
        Group0 = 1 << 0,
        Group1 = 1 << 1,
        Group2 = 1 << 2,
        Group3 = 1 << 3,
        Group4 = 1 << 4,
        Group5 = 1 << 5,
        Group6 = 1 << 6,
        Group7 = 1 << 7,
        Group8 = 1 << 8,
        Group9 = 1 << 9,
        Group10 = 1 << 10,
        Group11 = 1 << 11,
        Group12 = 1 << 12,
        Group13 = 1 << 13,
        Group14 = 1 << 14,
        Group15 = 1 << 15,
        Group16 = 1 << 16,
        Group17 = 1 << 17,
        Group18 = 1 << 18,
        Group19 = 1 << 19,
        Group20 = 1 << 20,
        Group21 = 1 << 21,
        Group22 = 1 << 22,
        Group23 = 1 << 23,
        Group24 = 1 << 24,
        Group25 = 1 << 25,
        Group26 = 1 << 26,
        Group27 = 1 << 27,
        Group28 = 1 << 28,
        Group29 = 1 << 29,
        Group30 = 1 << 30,
        Group31 = unchecked((uint)(1 << 31)),

        All = 0xffffffff,
    }
}
