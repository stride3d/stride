// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Media;

namespace Stride.Audio
{
    /// <summary>
    /// Interface for a playable sound.
    /// A playable sound can loop (ref <see cref="IsLooped"/>), be played (ref <see cref="Play"/>), be paused (ref <see cref="Pause"/>), be resumed (ref <see cref="Play"/>), 
    /// be stopped (ref <see cref="Stop()"/>) and be attenuated (ref <see cref="Volume"/>).
    /// To query the current state of a sound use the <see cref="PlayState"/> property. 
    /// To stop a sound after its currently loop use <see cref="ExitLoop"/>
    /// </summary>
    public interface IPlayableSound
    {
        /// <summary>
        /// The current state of the sound. 
        /// </summary>
        PlayState PlayState { get; }

        /// <summary>
        /// Gets or sets whether the sound is automatically looping from beginning when it reaches the end.
        /// </summary>
        bool IsLooping { get; set; }

        /// <summary>
        /// Start or resume playing the sound.
        /// </summary>
        /// <remarks>A call to Play when the sound is already playing has no effects.</remarks>
        void Play();

        /// <summary>
        /// Pause the sounds.
        /// </summary>
        /// <remarks>A call to Pause when the sound is already paused or stopped has no effects.</remarks>
        void Pause();

        /// <summary>
        /// Stop playing the sound immediately and reset the sound to the beginning of the track.
        /// </summary>
        /// <remarks>A call to Stop when the sound is already stopped has no effects</remarks>
        void Stop();

        /// <summary>
        /// The global volume at which the sound is played.
        /// </summary>
        /// <remarks>Volume is ranging from 0.0f (silence) to 1.0f (full volume). Values beyond those limits are clamped.</remarks>
        float Volume { get; set; }
    }
}
