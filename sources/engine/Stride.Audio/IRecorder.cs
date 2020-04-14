// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Audio
{
    /// <summary>
    /// <para>Interface for an audio input recorder</para>
    /// <para></para>
    /// </summary>
    //TODO improve documentation explaining how the interface works, especially what happens when the buffer is full (ring buffer/stops ??) when it will be implemented
    internal interface IRecorder
    {
        /// <summary>
        /// Gets or sets audio capture buffer duration. 
        /// </summary>
        // TODO documentation for Exception thrown when duration is not enough or correct
        TimeSpan BufferDuration { get; set; }

        /// <summary>
        /// Gets or sets audio capture buffer size in bytes. 
        /// </summary>
        // TODO documentation for Exception thrown when duration is not enough or correct
        TimeSpan BufferSize { get; set; }

        /// <summary>
        /// Returns the sample rate in Hertz (Hz) at which the microphone is capturing audio data. 
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// Returns the recording state of the recorder object. 
        /// </summary>
        RecorderState State { get; }

        /// <summary>
        /// Returns the duration of audio playback based on the hypotetical given size of the buffer. 
        /// </summary>
        /// <param name="sizeInBytes">Size, in bytes, of the audio data of which we want to know the duration.</param>
        /// <returns><see cref="TimeSpan"/> object that represents the duration of the audio playback.</returns>
        TimeSpan GetSampleDuration(int sizeInBytes);

        /// <summary>
        /// Returns the size of the byte array required to hold the specified duration of audio for this microphone object. 
        /// </summary>
        /// <param name="duration"><see cref="TimeSpan"/> object that contains the duration of the audio sample of which we want to know the size. </param>
        /// <returns>Size in bytes, of the audio buffer.</returns>
        int GetSampleSizeInBytes(TimeSpan duration);

        /// <summary>
        /// Starts microphone audio capture. 
        /// </summary>
        /// <exception cref="NoMicrophoneConnectedException">The microphone has been unplugged since the creation of the recorder instance</exception>
        void Start();

        /// <summary>
        /// Stops microphone audio capture. 
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets the latest recorded data from the microphone based on the audio capture buffer size.
        /// </summary>
        /// <param name="buffer">Buffer, in bytes, that will contain the captured audio data. The audio format is PCM wave data.</param>
        /// <returns>The buffer size, in bytes, of the audio data.</returns>
        /// <exception cref="ArgumentException">buffer is null, has zero length, or does not satisfy alignment requirements.</exception>
        int GetData(byte[] buffer);

        /// <summary>
        /// Gets the latest captured audio data from the microphone based on the specified offset and byte count.
        /// </summary>
        /// <param name="buffer">Buffer, in bytes, that will contain the captured audio data. The audio format is PCM wave data.</param>
        /// <param name="offset">Offset, in bytes, to the desired starting position of the data.</param>
        /// <param name="count">Amount, in bytes, of desired audio data.</param>
        /// <returns>The buffer size, in bytes, of the audio data.</returns>
        /// <exception cref="ArgumentException">
        /// The exception thrown when the following arguments are invalid: 
        /// <list type="bullet">
        /// <item>buffer is null, has zero length, or does not satisfy alignment requirements.</item>
        /// <item>offset is less than zero, is greater than or equal to the size of the buffer, or does not satisfy alignment requirements. </item>
        /// <item>The sum of count and offset is greater than the size of the buffer, count is less than or equal to zero, or does not satisfy alignment requirements. </item>
        /// </list>
        /// </exception>
        int GetData(byte[] buffer, int offset, int count);

        /// <summary>
        /// Event that occurs when the audio capture buffer is ready to processed. 
        /// </summary>
        event EventHandler<EventArgs> BufferReady;
    }
}
