// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1300 // Element must begin with upper-case letter
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Stride.Audio
{
    /// <summary>
    /// Wrapper around Celt
    /// </summary>
    internal class Celt : IDisposable
    {
        public int SampleRate { get; set; }

        public int BufferSize { get; set; }

        public int Channels { get; set; }

        private IntPtr celtPtr;

        static Celt()
        {
            NativeInvoke.PreLoad();
        }

        /// <summary>
        /// Initialize the Celt encoder/decoder
        /// </summary>
        /// <param name="sampleRate">Required sample rate</param>
        /// <param name="bufferSize">Required buffer size</param>
        /// <param name="channels">Required channels</param>
        /// <param name="decoderOnly">If we desire only to decode set this to true</param>
        public Celt(int sampleRate, int bufferSize, int channels, bool decoderOnly)
        {
            SampleRate = sampleRate;
            BufferSize = bufferSize;
            Channels = channels;
            celtPtr = xnCeltCreate(sampleRate, bufferSize, channels, decoderOnly);
            if (celtPtr == IntPtr.Zero)
            {
                throw new Exception("Failed to create an instance of the celt encoder/decoder.");
            }
        }

        /// <summary>
        /// Dispose the Celt encoder/decoder
        /// Do not call Encode or Decode after disposal!
        /// </summary>
        public void Dispose()
        {
            if (celtPtr != IntPtr.Zero)
            {
                xnCeltDestroy(celtPtr);
                celtPtr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Decodes compressed celt data into PCM 16 bit shorts
        /// </summary>
        /// <param name="inputBuffer">The input buffer</param>
        /// <param name="inputBufferSize">The size of the valid bytes in the input buffer</param>
        /// <param name="outputSamples">The output buffer, the size of frames should be the same amount that is contained in the input buffer</param>
        /// <returns></returns>
        public unsafe int Decode(byte[] inputBuffer, int inputBufferSize, short[] outputSamples)
        {
            fixed (short* samplesPtr = outputSamples)
            fixed (byte* bufferPtr = inputBuffer)
            {
                return xnCeltDecodeShort(celtPtr, bufferPtr, inputBufferSize, samplesPtr, outputSamples.Length / Channels);
            }
        }

        /// <summary>
        /// Decodes compressed celt data into PCM 16 bit shorts
        /// </summary>
        /// <param name="inputBuffer">The input buffer</param>
        /// <param name="inputBufferSize">The size of the valid bytes in the input buffer</param>
        /// <param name="outputSamples">The output buffer, the size of frames should be the same amount that is contained in the input buffer</param>
        /// <returns></returns>
        public unsafe int Decode(byte[] inputBuffer, int inputBufferSize, short* outputSamples)
        {
            fixed (byte* bufferPtr = inputBuffer)
            {
                return xnCeltDecodeShort(celtPtr, bufferPtr, inputBufferSize, outputSamples, BufferSize);
            }
        }

        /// <summary>
        /// Reset decoder state.
        /// </summary>
        public void ResetDecoder()
        {
            xnCeltResetDecoder(celtPtr);
        }

        /// <summary>
        /// Gets the delay between encoder and decoder (in number of samples). This should be skipped at the beginning of a decoded stream.
        /// </summary>
        /// <returns></returns>
        public int GetDecoderSampleDelay()
        {
            var delay = 0;
            if (xnCeltGetDecoderSampleDelay(celtPtr, ref delay) != 0)
                delay = 0;
            return delay;
        }

        /// <summary>
        /// Encode PCM audio into celt compressed format
        /// </summary>
        /// <param name="audioSamples">A buffer containing interleaved channels (as from constructor channels) and samples (can be any number of samples)</param>
        /// <param name="outputBuffer">An array of bytes, the size of the array will be the max possible size of the compressed packet</param>
        /// <returns></returns>
        public unsafe int Encode(short[] audioSamples, byte[] outputBuffer)
        {
            fixed (short* samplesPtr = audioSamples)
            fixed (byte* bufferPtr = outputBuffer)
            {
                return xnCeltEncodeShort(celtPtr, samplesPtr, audioSamples.Length / Channels, bufferPtr, outputBuffer.Length);
            }
        }

        /// <summary>
        /// Decodes compressed celt data into PCM 32 bit floats
        /// </summary>
        /// <param name="inputBuffer">The input buffer</param>
        /// <param name="inputBufferSize">The size of the valid bytes in the input buffer</param>
        /// <param name="outputSamples">The output buffer, the size of frames should be the same amount that is contained in the input buffer</param>
        /// <returns></returns>
        public unsafe int Decode(byte[] inputBuffer, int inputBufferSize, float[] outputSamples)
        {
            fixed (float* samplesPtr = outputSamples)
            fixed (byte* bufferPtr = inputBuffer)
            {
                return xnCeltDecodeFloat(celtPtr, bufferPtr, inputBufferSize, samplesPtr, outputSamples.Length / Channels);
            }
        }

        /// <summary>
        /// Encode PCM audio into celt compressed format
        /// </summary>
        /// <param name="audioSamples">A buffer containing interleaved channels (as from constructor channels) and samples (can be any number of samples)</param>
        /// <param name="outputBuffer">An array of bytes, the size of the array will be the max possible size of the compressed packet</param>
        /// <returns></returns>
        public unsafe int Encode(float[] audioSamples, byte[] outputBuffer)
        {
            fixed (float* samplesPtr = audioSamples)
            fixed (byte* bufferPtr = outputBuffer)
            {
                return xnCeltEncodeFloat(celtPtr, samplesPtr, audioSamples.Length / Channels, bufferPtr, outputBuffer.Length);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr xnCeltCreate(int sampleRate, int bufferSize, int channels, bool decoderOnly);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void xnCeltDestroy(IntPtr celt);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int xnCeltResetDecoder(IntPtr celt);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int xnCeltGetDecoderSampleDelay(IntPtr celt, ref int delay);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int xnCeltEncodeFloat(IntPtr celt, float* inputSamples, int numberOfInputSamples, byte* outputBuffer, int maxOutputSize);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int xnCeltDecodeFloat(IntPtr celt, byte* inputBuffer, int inputBufferSize, float* outputBuffer, int numberOfOutputSamples);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int xnCeltEncodeShort(IntPtr celt, short* inputSamples, int numberOfInputSamples, byte* outputBuffer, int maxOutputSize);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int xnCeltDecodeShort(IntPtr celt, byte* inputBuffer, int inputBufferSize, short* outputBuffer, int numberOfOutputSamples);
    }
}
