// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;

namespace Xenko.Assets.Physics
{
    [DataContract]
    public class HeightmapResizingParameters
    {
        [DataMember(0)]
        [DefaultValue(false)]
        public bool Enabled { get; set; }

        [DataMember(10)]
        [InlineProperty]
        public Int2 Size { get; set; } = new Int2(1024, 1024);
    }
}
