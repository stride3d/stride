// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Audio
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
