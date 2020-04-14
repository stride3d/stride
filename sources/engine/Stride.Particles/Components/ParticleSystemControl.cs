// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Particles.Components
{
    /// <summary>
    /// Edit-time controls for the <see cref="ParticleSystemComponent"/>
    /// </summary>
    [DataContract("ParticleSystemControl")]
    public class ParticleSystemControl
    {
        [DataMemberIgnore]
        private StateControl oldControl = StateControl.Play;

        [DataMemberIgnore]
        private float resetSeconds = 5f;

        [DataMemberIgnore]
        private float currentElapsedTime;


        /// <summary>
        /// Resets the <see cref="ParticleSystem"/> every X seconds, starting the simulation over again. Setting it to 0 means the particle system won't be resetted
        /// </summary>
        /// <userdoc>
        /// Resets the particle system every X seconds, starting the simulation over again. Setting it to 0 means the particle system won't be resetted
        /// </userdoc>
        [DataMember(20)]
        [Display("Reset after (seconds)")]
        public float ResetSeconds
        {
            get { return resetSeconds; }
            set
            {
                if (value >= 0)
                    resetSeconds = value;
            }
        }

        /// <summary>
        /// State control used to Play, Pause or Stop the <see cref="ParticleSystem"/>
        /// </summary>
        /// <userdoc>
        /// State control used to Play, Pause or Stop the particle system
        /// </userdoc>
        [DataMember(30)]
        public StateControl Control { get; set; } = StateControl.Play;

        /// <summary>
        /// Warm-up time is the amount of time the system should spend in background pre-simulation when capturing the thumbnail
        /// </summary>
        /// <userdoc>
        /// Warm-up time is the amount of time the system should spend in background pre-simulation when capturing the thumbnail
        /// </userdoc>
        [DataMember(40)]
        [Display("Thumbnail Warm-up")]
        [DataMemberRange(0, 5, 0.01, 1, 3)]
        [DefaultValue(0.5f)]
        public float ThumbnailWarmupTime { get; set; } = 0.5f;

        /// <summary>
        /// Update the control with delta time. It will pause or restart the <see cref="ParticleSystem"/> if necessary
        /// </summary>
        /// <param name="dt">Delta time elapsed since the last update call</param>
        /// <param name="particleSystem">The <see cref="ParticleSystem"/> which this control should manage</param>
        public void Update(float dt, ParticleSystem particleSystem)
        {
            // Check if state has changed
            if (oldControl != Control)
            {
                switch (Control)
                {
                    case StateControl.Play:
                        particleSystem.Play();
                        break;

                    case StateControl.Pause:
                        particleSystem.Pause();
                        break;

                    case StateControl.Stop:
                        particleSystem.Stop();
                        break;
                }

                oldControl = Control;
            }

            // If the particle system is not currently playing, skip updating the time
            if (Control != StateControl.Play)
                return;

            currentElapsedTime += dt;

            if (resetSeconds <= 0)
                return;

            if (currentElapsedTime >= resetSeconds)
            {
                while (currentElapsedTime >= resetSeconds)
                    currentElapsedTime -= resetSeconds;

                particleSystem.ResetSimulation();
            }
        }
    }
}
