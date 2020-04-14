// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A collection of tags.
    /// </summary>
    [DataContract("TagCollection")]
    public class TagCollection : List<string>
    {
    }
}
