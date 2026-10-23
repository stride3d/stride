// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Audio
{
    /// <summary>
    /// Cross-platform audio device abstraction. Static method bodies live in platform-specific partials:
    /// AudioLayer.OpenAL.cs (Windows/Linux/Android/iOS — DllImport into libstrideaudio),
    /// AudioLayer.AVFoundation.cs (iOS/macOS — managed AVAudioEngine implementation).
    /// </summary>
    public partial class AudioLayer
    {
        public struct Device
        {
            public IntPtr Ptr;
        }

        public struct Listener
        {
            public IntPtr Ptr;
        }

        public struct Source
        {
            public IntPtr Ptr;
        }

        public struct Buffer
        {
            public IntPtr Ptr;
        }

        public enum DeviceFlags
        {
            None,
            Hrtf,
        }

        public enum BufferType
        {
            None,
            BeginOfStream,
            EndOfStream,
            EndOfLoop,
        }
    }
}
