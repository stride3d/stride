// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    ///   Represents a display subsystem (including one or more GPUs, DACs and video memory).
    ///   A display subsystem is often referred to as a video card, however, on some machines the display subsystem is part of the motherboard.
    /// </summary>
    public sealed partial class GraphicsAdapter : ComponentBase
    {
        /// <summary>
        ///   Gets the <see cref="GraphicsOutput"/>s attached to this adapter.
        /// </summary>
        public GraphicsOutput[] Outputs { get; }

        /// <summary>
        ///   Gets the unique identifier of this <see cref="GraphicsAdapter"/>.
        /// </summary>
        public long AdapterUid { get; internal set; }


        /// <summary>
        ///   Returns the description of this <see cref="GraphicsAdapter"/>.
        /// </summary>
        public override string ToString()
        {
            return Description;
        }
    }
}
