// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Audio
{
    /// <summary>
    /// Interface for 3D localizable sound.
    /// The interface currently supports only mono and stereo sounds (ref <see cref="Pan"/>).
    /// The user can localize its sound with <see cref="Apply3D"/> by creating one <see cref="AudioEmitter"/> 
    /// and one <see cref="AudioListener"/> respectly corresponding to the source of the sound and the listener.
    /// The <see cref="Pan"/> function enable the user to change the distribution of sound between the left and right speakers.
    /// </summary>
    /// <remarks>Functions <see cref="Pan"/> and <see cref="Apply3D"/> cannot be used together. 
    /// A call to <see cref="Apply3D"/> will reset <see cref="Pan"/> to its default value and inverse.</remarks>
    /// <seealso cref="Sound"/>
    /// <seealso cref="SoundInstance"/>
    public interface IPositionableSound : IPlayableSound
    {
        /// <summary>
        /// Set the sound balance between left and right speaker.
        /// </summary>
        /// <remarks>Panning is ranging from -1.0f (full left) to 1.0f (full right). 0.0f is centered. Values beyond this range are clamped. 
        /// Panning modifies the total energy of the signal (Pan == -1 => Energy = 1 + 0, Pan == 0 => Energy = 1 + 1, Pan == 0.5 => Energy = 1 + 0.5, ...) 
        /// <para>A call to <see cref="Pan"/> cancels the effect of Apply3D.</para></remarks>
        float Pan { get; set; }

        /// <summary>
        /// Gets or sets the pitch of the sound, might conflict with spatialized sound spatialization.
        /// </summary>
        float Pitch { get; set; }

        /// <summary>
        /// Applies 3D positioning to the sound. 
        /// More precisely adjust the channel volumes and pitch of the sound, 
        /// such that the sound source seems to come from the <paramref name="emitter"/> to the listener/>.
        /// </summary>
        /// <param name="emitter">The emitter that correspond to this sound</param>
        /// <remarks>
        /// <see cref="Apply3D"/> can be used only on mono-sounds.
        /// <para>A call to <see cref="Apply3D"/> reset <see cref="Pan"/> to its default values.</para>
        /// <para>A call to <see cref="Apply3D"/> does not modify the value of <see cref="IPlayableSound.Volume"/>, 
        /// the effective volume of the sound is a combination of the two effects.</para>
        /// <para>
        /// The final resulting pitch depends on the listener and emitter relative velocity. 
        /// The final resulting channel volumes depend on the listener and emitter relative positions and the value of <see cref="IPlayableSound.Volume"/>. 
        /// </para>
        /// </remarks>
        void Apply3D(AudioEmitter emitter);
    }
}
