// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1300 // Element must begin with upper-case letter
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Stride.Audio
{
    /// <summary>
    /// Wrapper around Celt
    /// </summary>
    internal unsafe class Celt : IDisposable
    {
        public int SampleRate { get; set; }

        public int BufferSize { get; set; }

        public int Channels { get; set; }

        private OpusCustomMode* mode;
        private OpusCustomEncoder* encoder;
        private OpusCustomDecoder* decoder;
        
        private static readonly int OPUS_RESET_STATE = 4028;

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
            var result = xnCeltCreate(sampleRate, bufferSize, channels, decoderOnly);
            if (result == false)
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
            xnCeltDestroy();    
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
            Debug.Assert((uint)inputBufferSize <= (uint)inputBuffer.Length);
            fixed (short* samplesPtr = outputSamples)
            fixed (byte* bufferPtr = inputBuffer)
            {
                return xnCeltDecodeShort(decoder, bufferPtr, inputBufferSize, samplesPtr, outputSamples.Length / Channels);
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
            Debug.Assert((uint)inputBufferSize <= (uint)inputBuffer.Length);
            fixed (byte* bufferPtr = inputBuffer)
            {
                return xnCeltDecodeShort(decoder, bufferPtr, inputBufferSize, outputSamples, BufferSize);
            }
        }

        /// <summary>
        /// Reset decoder state.
        /// </summary>
        public void ResetDecoder()
        {
            xnCeltResetDecoder(decoder);
        }

        /// <summary>
        /// Gets the delay between encoder and decoder (in number of samples). This should be skipped at the beginning of a decoded stream.
        /// </summary>
        /// <returns></returns>
        public int GetDecoderSampleDelay()
        {
            var delay = 0;
            if (xnCeltGetDecoderSampleDelay(decoder, ref delay) != 0)
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
                return xnCeltEncodeShort(encoder, samplesPtr, audioSamples.Length / Channels, bufferPtr, outputBuffer.Length);
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
                return xnCeltDecodeFloat(decoder, bufferPtr, inputBufferSize, samplesPtr, outputSamples.Length / Channels);
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
                return xnCeltEncodeFloat(encoder, samplesPtr, audioSamples.Length / Channels, bufferPtr, outputBuffer.Length);
            }
        }

        private bool xnCeltCreate(int sampleRate, int bufferSize, int channels, bool decoderOnly)
        {
            mode = opus_custom_mode_create(sampleRate, bufferSize, null);
            if (mode == null) return false;

            decoder = opus_custom_decoder_create(mode, channels, null);
            if (decoder == null) return false;

            if (!decoderOnly)
            {
                encoder = opus_custom_encoder_create(mode, channels, null);
                if (encoder  == null) return false;
            }
            return true;
        }

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern OpusCustomEncoder* opus_custom_encoder_create(OpusCustomMode* mode, int channels, int* error);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern OpusCustomDecoder* opus_custom_decoder_create(OpusCustomMode* mode, int channels, int* error);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern OpusCustomMode* opus_custom_mode_create(int sampleRate, int bufferSize, int* error);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern int opus_custom_decoder_ctl(OpusCustomDecoder* decoder, int request, params nint[] args);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern void opus_custom_encoder_destroy(OpusCustomEncoder* encoder);
        
        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern void opus_custom_decoder_destroy(OpusCustomDecoder* encoder);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern void opus_custom_mode_destroy(OpusCustomMode* encoder);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern int opus_custom_encode_float(OpusCustomEncoder* encoder, float* inputSamples, int numberOfInputSamples, byte* outputBuffer, int maxOutputSize);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern int opus_custom_decode_float(OpusCustomDecoder* decoder, byte* inputBuffer, int inputBufferSize, float* outputBuffer, int numberOfOutputSamples);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern int opus_custom_encode(OpusCustomEncoder* encoder, short* inputSamples, int numberOfInputSamples, byte* outputBuffer, int maxOutputSize);
        
        [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
        private static extern int opus_custom_decode(OpusCustomDecoder* decoder, byte* inputBuffer, int inputBufferSize, short* outputBuffer, int numberOfOutputSamples);

        private void xnCeltDestroy()
        {
            if (encoder != null) 
                opus_custom_encoder_destroy(encoder);
            encoder = null;
            if (decoder != null) 
                opus_custom_decoder_destroy(decoder);
            decoder = null;
            if (mode != null) 
                opus_custom_mode_destroy(mode);
            mode = null;
        }

        private static int xnCeltResetDecoder(OpusCustomDecoder* decoder)
        {
            return opus_custom_decoder_ctl(decoder, (int)OpusRequest.ResetState);
        }

        private static int xnCeltGetDecoderSampleDelay(OpusCustomDecoder* decoder, ref int delay)
        {
            return opus_custom_decoder_ctl(decoder, (int)OpusRequest.LookAhead , delay);
        }

        private static unsafe int xnCeltEncodeFloat(OpusCustomEncoder* encoder, float* inputSamples, int numberOfInputSamples, byte* outputBuffer, int maxOutputSize)
        {
            return opus_custom_encode_float(encoder, inputSamples, numberOfInputSamples, outputBuffer, maxOutputSize);
        }

        private static unsafe int xnCeltDecodeFloat(OpusCustomDecoder* decoder, byte* inputBuffer, int inputBufferSize, float* outputBuffer, int numberOfOutputSamples)
        {
            return opus_custom_decode_float(decoder, inputBuffer, inputBufferSize, outputBuffer, numberOfOutputSamples);
        }

        private static unsafe int xnCeltEncodeShort(OpusCustomEncoder* encoder, short* inputSamples, int numberOfInputSamples, byte* outputBuffer, int maxOutputSize)
        {
            return opus_custom_encode(encoder, inputSamples, numberOfInputSamples, outputBuffer, maxOutputSize);
        }

        private static unsafe int xnCeltDecodeShort(OpusCustomDecoder* decoder, byte* inputBuffer, int inputBufferSize, short* outputBuffer, int numberOfOutputSamples)
        {
            return opus_custom_decode(decoder, inputBuffer, inputBufferSize, outputBuffer, numberOfOutputSamples);
        }
    }

    internal enum OpusRequest
    {
        LookAhead = 4027,
        ResetState = 4028
    }

    internal class OpusCustomEncoder
    {
    }

    internal class OpusCustomDecoder
    {
    }

    internal class OpusCustomMode
    {
    }
}
