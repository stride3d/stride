// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Native;

namespace Stride.Audio
{
    /// <summary>
    /// Represents a 3D audio emitter in the audio scene. 
    /// This object, used in combination with an <see cref="AudioListener"/>, can simulate 3D audio localization effects for a given sound implementing the <see cref="IPositionableSound"/> interface.
    /// For more details take a look at the <see cref="IPositionableSound.Apply3D"/> function.
    /// </summary>
    /// <seealso cref="IPositionableSound.Apply3D"/>
    /// <seealso cref="AudioListener"/>
    public class AudioEmitter
    {
        /// <summary>
        /// The position of the emitter in the 3D world.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The velocity of the emitter in the 3D world. 
        /// </summary>
        /// <remarks>This is only used to calculate the doppler effect on the sound effect</remarks>
        public Vector3 Velocity;

        private Vector3 up;

        /// <summary>
        /// Gets or sets the Up orientation vector for this emitter. This vector up of the world for the emitter.
        /// </summary>
        /// <remarks>
        /// <para>By default, this value is (0,1,0).</para>
        /// <para>The value provided will be normalized if it is not already.</para>
        /// <para>The values of the Forward and Up vectors must be orthonormal (at right angles to one another). 
        /// Behavior is undefined if these vectors are not orthonormal.</para>
        /// <para>Doppler and Matrix values between an <see name="AudioEmitter"/> and an <see cref="AudioEmitter"/> are effected by the emitter orientation.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The value provided to the set accessor is (0,0,0).</exception>
        public Vector3 Up
        {
            get
            {
                return up;
            }
            set
            {
                if (value == Vector3.Zero)
                    throw new InvalidOperationException("The value of the Up vector can not be (0,0,0)");

                up = Vector3.Normalize(value);
            }
        }

        private Vector3 forward;

        /// <summary>
        /// Gets or sets the forward orientation vector for this emitter. This vector represents the orientation the emitter is looking at.
        /// </summary>
        /// <remarks>
        /// <para>By default, this value is (0,0,1).</para>
        /// <para>The value provided will be normalized if it is not already.</para>
        /// <para>The values of the Forward and Up vectors must be orthonormal (at right angles to one another). 
        /// Behavior is undefined if these vectors are not orthonormal.</para>
        /// <para>Doppler and Matrix values between an <see name="AudioEmitter"/> and an <see cref="AudioEmitter"/> are effected by the emitter orientation.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The value provided to the set accessor is (0,0,0) or <see cref="Up"/>.</exception>
        public Vector3 Forward
        {
            get
            {
                return forward;
            }
            set
            {
                if (value == Vector3.Zero)
                    throw new InvalidOperationException("The value of the Forward vector can not be (0,0,0)");

                forward = Vector3.Normalize(value);
            }
        }

        internal Matrix WorldTransform;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioEmitter"/> class.
        /// </summary>
        public AudioEmitter()
        {
            Forward = new Vector3(0, 0, 1);
            Up = new Vector3(0, 1, 0);
        }

        internal void Apply3D(AudioLayer.Source source)
        {
            AudioLayer.SourcePush3D(source, ref Position, ref forward, ref up, ref Velocity, ref WorldTransform);
        }
    }
}
