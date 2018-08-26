// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Media
{
    /// <summary>
    /// Interface for playable media
    /// </summary>
    public interface IMediaPlayer : IMediaReader
    {
        /// <summary>
        /// Start or resume playing the media.
        /// </summary>
        /// <remarks>A call to Play when the media is already playing has no effects.</remarks>
        void Play();

        /// <summary>
        /// Pause the media.
        /// </summary>
        /// <remarks>A call to Pause when the media is already paused or stopped has no effects.</remarks>
        void Pause();

        /// <summary>
        /// Stop playing the media immediately and reset the media to the beginning of the source.
        /// </summary>
        /// <remarks>A call to Stop when the media is already stopped has no effects</remarks>
        void Stop();
    }
}
