// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Physics;
using Stride.Assets;

namespace Stride.Physics.Assets
{
    [DataContract("FloatHeightmapHeightConversionParameters")]
    [DataAlias("Stride.Assets.Physics.FloatHeightmapHeightConversionParamters,Stride.Assets")]
    [Display("Float")]
    public class FloatHeightmapHeightConversionParamters : IHeightmapHeightConversionParameters
    {
        [DataMemberIgnore]
        public HeightfieldTypes HeightType => HeightfieldTypes.Float;

        [DataMember(10)]
        public Vector2 HeightRange { get; set; } = new Vector2(-10, 10);

        [DataMemberIgnore]
        public float HeightScale => 1f;
    }
}
