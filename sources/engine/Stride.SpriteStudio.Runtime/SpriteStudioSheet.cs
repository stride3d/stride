// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;

namespace Stride.SpriteStudio.Runtime
{
    [DataContract]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<SpriteStudioSheet>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<SpriteStudioSheet>))]
    public class SpriteStudioSheet
    {
        public List<SpriteStudioNode> NodesInfo { get; set; }

        public SpriteSheet SpriteSheet { get; set; }

        [DataMemberIgnore]
        public Sprite[] Sprites { get; internal set; }
    }
}
