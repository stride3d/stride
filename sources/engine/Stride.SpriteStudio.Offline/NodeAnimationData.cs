// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.SpriteStudio.Offline
{
    [DataContract]
    public class NodeAnimationData
    {
        public NodeAnimationData()
        {
            Data = new Dictionary<string, List<Dictionary<string, string>>>();
        }

        public Dictionary<string, List<Dictionary<string, string>>> Data;
    }
}
