// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Audio
{
    /// <summary>
    /// Reprensent an Audio Hardware Device.
    /// Can be used when creating an <see cref="AudioEngine"/> to specify the device on which to play the sound.
    /// </summary>
    public class AudioDevice
    {
        /// <summary>
        /// Returns the name of the current device.
        /// </summary>
        public string Name { get; set; }

        public AudioDevice()
        {
            Name = "default";
        }
    }
}
