// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// This class represents a graphics adapter.
    /// </summary>
    public sealed partial class GraphicsAdapter : ComponentBase
    {
        /// <summary>
        ///   Gets the <see cref="GraphicsOutput"/>s attached to this adapter.
        /// </summary>
        public GraphicsOutput[] Outputs { get; }

        /// <summary>
        /// Return the description of this adapter
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        ///   Gets the unique id of this device.
        /// </summary>
        public long AdapterUid { get; internal set; }
    }
}
