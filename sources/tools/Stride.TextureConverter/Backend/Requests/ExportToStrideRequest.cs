// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Stride.Graphics;

namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// Request to export a texture to a Stride <see cref="Image"/> instance.
    /// </summary>
    internal class ExportToStrideRequest : IRequest
    {

        public override RequestType Type { get { return RequestType.ExportToStride; } }

        /// <summary>
        /// The stride <see cref="Image"/> which will contains the exported texture.
        /// </summary>
        public Image XkImage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportToStrideRequest"/> class.
        /// </summary>
        public ExportToStrideRequest()
        {
        }
    }
}
