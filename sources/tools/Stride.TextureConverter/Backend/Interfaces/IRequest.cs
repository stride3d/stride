// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.TextureConverter
{
    internal abstract class IRequest
    {
        /// <summary>
        /// THe request type, corresponding to the enum <see cref="RequestType"/>
        /// </summary>
        /// <value>
        /// The type of the request.
        /// </value>
        public abstract RequestType Type { get; }
    }
}
