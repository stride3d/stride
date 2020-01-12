// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Physics;

namespace Xenko.Assets.Physics
{
    [DataContract]
    [Display("Float")]
    public class FloatHeightmapHeightConversionParamters : IHeightmapHeightConversionParameters
    {
        [DataMemberIgnore]
        public HeightfieldTypes HeightType => HeightfieldTypes.Float;

        [DataMember(10)]
        public Vector2 HeightRange { get; set; } = new Vector2(-10, 10);

        [DataMemberIgnore]
        public float HeightScale => 1f;

        [DataMember(20)]
        public bool ScaleToFit { get; set; } = true;
    }
}
