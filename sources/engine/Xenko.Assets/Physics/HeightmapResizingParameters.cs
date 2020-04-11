// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;

namespace Xenko.Assets.Physics
{
    [DataContract]
    public struct HeightmapResizingParameters
    {
        [DataMember(0)]
        public bool Enabled { get; set; }

        /// <summary>
        /// New size of the heightmap.
        /// </summary>
        [DataMember(10)]
        [InlineProperty]
        public Int2 Size { get; set; }
    }
}
