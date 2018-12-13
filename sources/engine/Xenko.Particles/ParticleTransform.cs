// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Particles
{
    [DataContract("ParticleTransform")]

    public class ParticleTransform
    {
        [DataMember(0)]
        [Display("Position inheritance")]
        public bool InheritPosition { get; set; } = true;

        [DataMember(1)]
        [Display("Position offset")]
        public Vector3 Position { get; set; } = new Vector3(0, 0, 0);

        [DataMember(2)]
        [Display("Rotation inheritance")]
        public bool InheritRotation { get; set; } = true;

        [DataMember(3)]
        [Display("Rotation offset")]
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        [DataMember(4)]
        [Display("Scale inheritance")]
        public bool InheritScale { get; set; } = true;

        [DataMember(5)]
        [Display("Scale offset")]
        public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);

        [DataMember(6)]
        [Display("Uniform Scale")]
        public float ScaleUniform { get; set; } = 1f;

        // Order of these members should be *after* the fields they control (own offset and inherited field)
        [DataMember(10)]
        public bool DisplayParticlePosition = false;

        [DataMember(11)]
        public bool DisplayParticleRotation = false;

        [DataMember(12)]
        public bool DisplayParticleScale = false;

        [DataMember(13)]
        public bool DisplayParticleScaleUniform = false;

        [DataMemberIgnore]
        public Vector3 WorldPosition { get; private set; } = new Vector3(0, 0, 0);

        [DataMemberIgnore]
        public Quaternion WorldRotation { get; private set; } = Quaternion.Identity;

        [DataMemberIgnore]
        public Vector3 WorldScale { get; private set; } = new Vector3(1, 1, 1);


        public void SetParentTransform(ParticleTransform parentTransform)
        {
            var notNull = (parentTransform != null);

            var ownScale = Scale * ScaleUniform;
            WorldScale = (notNull && InheritScale) ? ownScale * parentTransform.WorldScale.X : ownScale;

            WorldRotation = (notNull && InheritRotation) ? Rotation * parentTransform.WorldRotation : Rotation;

            var offsetTranslation = Position * ((notNull && InheritScale) ? parentTransform.WorldScale.X : 1f);

            if (notNull && InheritRotation)
            {
                parentTransform.WorldRotation.Rotate(ref offsetTranslation);
            }

            WorldPosition = (notNull && InheritPosition) ? parentTransform.WorldPosition + offsetTranslation : offsetTranslation;
        }
    }
}

