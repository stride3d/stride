using System;
using Stride.Core.Mathematics;

namespace Stride.Core.Assets
{
    [DataContract]
    public class MorphTargetAsset
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Vector3[] VertexOffsets { get; set; }

        [DataMember]
        public float[] Weights { get; set; }
    }
}
