// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Assets.Physics
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
