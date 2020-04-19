// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Particles.Updaters.FieldShapes
{
    [DataContract("FieldFalloff")]
    [Display("Falloff")]
    public class FieldFalloff
    {
        /// <summary>
        /// The strength of the force in the center of the bounding shape.
        /// </summary>
        /// <userdoc>
        /// The strength of the force in the center of the bounding shape.
        /// </userdoc>
        [DataMember(10)]
        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Strength inside")]
        public float StrengthInside { get; set; } = 1f;

        /// <summary>
        /// After this relative distance from the center, the force strength will start to change
        /// </summary>
        /// <userdoc>
        /// After this relative distance from the center, the force strength will start to change
        /// </userdoc>
        [DataMember(20)]
        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Falloff start")]
        public float FalloffStart { get; set; } = 0.1f;

        /// <summary>
        /// The strength of the force outside the bounding shape.
        /// </summary>
        /// <userdoc>
        /// The strength of the force outside the bounding shape.
        /// </userdoc>
        [DataMember(30)]
        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Strength outside")]
        public float StrengthOutside { get; set; } = 0f;


        /// <summary>
        /// After this relative distance from the center, the force strength will be equal to [Strength outside]
        /// </summary>
        /// <userdoc>
        /// After this relative distance from the center, the force strength will be equal to [Strength outside]
        /// </userdoc>
        [DataMember(40)]
        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Falloff end")]
        public float FalloffEnd { get; set; } = 0.9f;

        /// <summary>
        /// Get interpolated strength based on relative distance from the center (lerp)
        /// </summary>
        /// <param name="inDistance"></param>
        /// <returns></returns>
        public float GetStrength(float inDistance)
        {
            if (inDistance <= FalloffStart)
                return StrengthInside;

            if (inDistance >= FalloffEnd)
                return StrengthOutside;

            var lerp = (inDistance - FalloffStart) / (FalloffEnd / FalloffStart);
            return StrengthInside + (StrengthOutside - StrengthInside) * lerp;
        }
    }
}
