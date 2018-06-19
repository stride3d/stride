// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// This class represents a graphics adapter.
    /// </summary>
    public sealed partial class GraphicsAdapter : ComponentBase
    {
        private readonly GraphicsOutput[] outputs;

        /// <summary>
        /// Gets the <see cref="GraphicsOutput"/> attached to this adapter
        /// </summary>
        /// <returns>The <see cref="GraphicsOutput"/> attached to this adapter.</returns>
        public GraphicsOutput[] Outputs
        {
            get
            {
                return outputs;
            }
        }

        /// <summary>
        /// Return the description of this adapter
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        /// The unique id in the form of string of this device
        /// </summary>
        public string AdapterUid { get; internal set; }
    }
}
