// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Audio
{
    /// <summary>
    /// The exception that is thrown when an internal error happened in the Audio System. That is an error that is not due to the user behavior.
    /// </summary>
    public class AudioEngineInternalExceptions : Exception
    {
        internal AudioEngineInternalExceptions(string msg)
            : base("An internal error happened in the audio engine [details:'" + msg + "'")
        { }
    }
}
