// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Audio
{
    internal partial class Microphone
    {
        /// <summary>
        /// Create a new instance of Microphone ready for recording.
        /// </summary>
        /// <exception cref="NoMicrophoneConnectedException">No microphone is currently plugged.</exception>
        public Microphone()
        {
            throw new NotImplementedException();
        }

        #region Implementation of the IRecorder interface

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
