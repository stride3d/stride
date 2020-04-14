// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Particles.Initializers
{
    /// <summary>
    /// This initializer sets each field to its default value in case a custom initializer is not present.
    /// </summary>
    public class InitialDefaultFields
    {
        // TODO This will have to change at some point when we have better idea of how customizable the fields will be.
        //  Ideally no field should get initialized twice

        public unsafe void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            // Position - defaults to World position of the emitter
            var posField = pool.GetField(ParticleFields.Position);
            if (posField.IsValid())
            {
                for (var i = startIdx; i != endIdx;)
                {
                    var particle = pool.FromIndex(i);
                    (*((Vector3*)particle[posField])) = WorldPosition;
                    i = (i + 1) % maxCapacity;
                }
            }

            // Old position - defaults to World position of the emitter
            var oldPosField = pool.GetField(ParticleFields.OldPosition);
            if (oldPosField.IsValid())
            {
                for (var i = startIdx; i != endIdx;)
                {
                    var particle = pool.FromIndex(i);
                    (*((Vector3*)particle[oldPosField])) = WorldPosition;
                    i = (i + 1) % maxCapacity;
                }
            }

            // Direction - defaults to (0, 0, 0)
            var dirField = pool.GetField(ParticleFields.Direction);
            if (dirField.IsValid())
            {
                var zeroDirection = Vector3.Zero;
                for (var i = startIdx; i != endIdx;)
                {
                    var particle = pool.FromIndex(i);
                    (*((Vector3*)particle[dirField])) = zeroDirection;
                    i = (i + 1) % maxCapacity;
                }
            }

            // Quaternion rotation - defaults to World rotation of the emitter
            var quatField = pool.GetField(ParticleFields.Quaternion);
            if (quatField.IsValid())
            {
                for (var i = startIdx; i != endIdx;)
                {
                    var particle = pool.FromIndex(i);
                    (*((Quaternion*)particle[quatField])) = WorldRotation;
                    i = (i + 1) % maxCapacity;
                }
            }

            // Angular rotation - defaults to 0
            var rotField = pool.GetField(ParticleFields.Rotation);
            if (rotField.IsValid())
            {
                for (var i = startIdx; i != endIdx;)
                {
                    var particle = pool.FromIndex(i);
                    (*((float*)particle[rotField])) = 0;
                    i = (i + 1) % maxCapacity;
                }
            }

            // Velocity - defaults to (0, 0, 0)
            var velField = pool.GetField(ParticleFields.Velocity);
            if (velField.IsValid())
            {
                var zeroVelocity = Vector3.Zero;
                for (var i = startIdx; i != endIdx;)
                {
                    var particle = pool.FromIndex(i);
                    (*((Vector3*)particle[velField])) = zeroVelocity;
                    i = (i + 1) % maxCapacity;
                }
            }

            // Size - defaults to the world scale of the emitter
            var sizeField = pool.GetField(ParticleFields.Size);
            if (sizeField.IsValid())
            {
                for (var i = startIdx; i != endIdx;)
                {
                    var particle = pool.FromIndex(i);
                    (*((float*)particle[sizeField])) = WorldScale;
                    i = (i + 1) % maxCapacity;
                }
            }

            // Velocity - defaults to (0, 0, 0)
            var colField = pool.GetField(ParticleFields.Color);
            if (colField.IsValid())
            {
                var whiteColor = new Color4(1,1,1,1);
                for (var i = startIdx; i != endIdx;)
                {
                    var particle = pool.FromIndex(i);
                    (*((Color4*)particle[colField])) = whiteColor;
                    i = (i + 1) % maxCapacity;
                }
            }

            // ChildrenFlags fields
            for (int j = 0; j < ParticleFields.ChildrenFlags.Length; j++)
            {
                var flagField = pool.GetField(ParticleFields.ChildrenFlags[j]);
                if (flagField.IsValid())
                {
                    for (var i = startIdx; i != endIdx;)
                    {
                        var particle = pool.FromIndex(i);
                        (*((uint*)particle[flagField])) = 0;
                        i = (i + 1) % maxCapacity;
                    }
                }

            }
        }

        [DataMemberIgnore]
        public Vector3 WorldPosition { get; private set; } = Vector3.Zero;
        [DataMemberIgnore]
        public Quaternion WorldRotation { get; private set; } = Quaternion.Identity;
        [DataMemberIgnore]
        public float WorldScale { get; private set; } = 1f;

        public void SetParentTRS(ref Vector3 translation, ref Quaternion rotation, float scale)
        {
            WorldScale = scale;

            WorldRotation = rotation;

            WorldPosition = translation;
        }
    }
}
