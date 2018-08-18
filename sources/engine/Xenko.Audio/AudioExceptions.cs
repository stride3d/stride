// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
using System;

namespace Xenko.Audio
{
    /// <summary>
    /// The exception that is thrown when audio engine failed to initialized.
    /// Most of the time is happens when no hardware is present, or when audio hardware is installed, but it is not enabled or where there is no output connected. 
    /// </summary>
    public class AudioInitializationException : Exception
    {
        internal AudioInitializationException()
            : base("Initialization of the audio engine failed. This may be due to missing audio hardware or missing connected audio outputs.")
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when <see cref="Microphone"/> API calls are made on a disconnected microphone. 
    /// </summary>
    public class NoMicrophoneConnectedException : Exception
    {
        internal NoMicrophoneConnectedException()
            : base("No microphone is currently connected.")
        { }
    }

    /// <summary>
    /// The exception that is thrown when the audio device became unusable through being unplugged or some other event.
    /// </summary>
    public class AudioDeviceInvalidatedException : Exception
    {
        internal AudioDeviceInvalidatedException()
            : base("The audio device became unusable through being unplugged or some other event.")
        { }
    }

    /// <summary>
    /// The exception that is thrown when an internal error happened in the Audio System. That is an error that is not due to the user behaviour.
    /// </summary>
    public class AudioSystemInternalException : Exception
    {
        internal AudioSystemInternalException(string msg)
            : base("An internal error happened in the audio system [details:'" + msg + "'")
        { }
    }
}
