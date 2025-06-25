// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
public enum StencilOperation
{
    /// <summary>
    /// </summary>
    Keep = 1,

    Zero = 2,

    Replace = 3,

    IncrementSaturation = 4,

    DecrementSaturation = 5,

    Invert = 6,

    Increment = 7,

    Decrement = 8
}
