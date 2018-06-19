// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xenko.Graphics;

namespace Xenko.TextureConverter.Requests
{
    /// <summary>
    /// Request to export a texture to a Xenko <see cref="Image"/> instance.
    /// </summary>
    internal class ExportToXenkoRequest : IRequest
    {

        public override RequestType Type { get { return RequestType.ExportToXenko; } }

        /// <summary>
        /// The xenko <see cref="Image"/> which will contains the exported texture.
        /// </summary>
        public Image XkImage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportToXenkoRequest"/> class.
        /// </summary>
        public ExportToXenkoRequest()
        {
        }
    }
}
