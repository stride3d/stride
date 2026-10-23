// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_MACOS
#pragma warning disable SA1300
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Stride.Audio
{
    /// <summary>
    /// Managed Celt wrapper that calls libCelt's <c>opus_custom_*</c> symbols directly.
    /// Replaces the libstrideaudio xnCelt* shim on Apple platforms.
    /// </summary>
    internal class Celt : IDisposable
    {
        // iOS: static-only linkage, libCelt.a is force-loaded into the app binary, __Internal hits dlsym(RTLD_DEFAULT).
        // macOS: libstrideaudio.dylib statically embeds libCelt at engine build time, exposing opus_custom_* as
        // dynamic exports — DllImport("strideaudio") resolves them at runtime via the loaded dylib.
#if STRIDE_PLATFORM_IOS
        private const string LibCelt = "__Internal";
#else
        private const string LibCelt = "strideaudio";
#endif

        // OPUS control request codes, copied from deps/Celt/include/opus_defines.h.
        private const int OPUS_RESET_STATE = 4028;
        private const int OPUS_GET_LOOKAHEAD_REQUEST = 4027;

        public int SampleRate { get; set; }
        public int BufferSize { get; set; }
        public int Channels { get; set; }

        private IntPtr mode;
        private IntPtr decoder;
        private IntPtr encoder;

        public Celt(int sampleRate, int bufferSize, int channels, bool decoderOnly)
        {
            SampleRate = sampleRate;
            BufferSize = bufferSize;
            Channels = channels;

            mode = opus_custom_mode_create(sampleRate, bufferSize, IntPtr.Zero);
            if (mode == IntPtr.Zero)
                throw new Exception("Failed to create Celt custom mode.");

            decoder = opus_custom_decoder_create(mode, channels, IntPtr.Zero);
            if (decoder == IntPtr.Zero)
                throw new Exception("Failed to create Celt decoder.");

            if (!decoderOnly)
            {
                encoder = opus_custom_encoder_create(mode, channels, IntPtr.Zero);
                if (encoder == IntPtr.Zero)
                    throw new Exception("Failed to create Celt encoder.");
            }
        }

        public void Dispose()
        {
            if (encoder != IntPtr.Zero) { opus_custom_encoder_destroy(encoder); encoder = IntPtr.Zero; }
            if (decoder != IntPtr.Zero) { opus_custom_decoder_destroy(decoder); decoder = IntPtr.Zero; }
            if (mode != IntPtr.Zero) { opus_custom_mode_destroy(mode); mode = IntPtr.Zero; }
        }

        public unsafe int Decode(byte[] inputBuffer, int inputBufferSize, short[] outputSamples)
        {
            Debug.Assert((uint)inputBufferSize <= (uint)inputBuffer.Length);
            fixed (short* samplesPtr = outputSamples)
            fixed (byte* bufferPtr = inputBuffer)
            {
                return opus_custom_decode(decoder, bufferPtr, inputBufferSize, samplesPtr, outputSamples.Length / Channels);
            }
        }

        public unsafe int Decode(byte[] inputBuffer, int inputBufferSize, short* outputSamples)
        {
            Debug.Assert((uint)inputBufferSize <= (uint)inputBuffer.Length);
            fixed (byte* bufferPtr = inputBuffer)
            {
                return opus_custom_decode(decoder, bufferPtr, inputBufferSize, outputSamples, BufferSize);
            }
        }

        public unsafe int Decode(byte[] inputBuffer, int inputBufferSize, float[] outputSamples)
        {
            fixed (float* samplesPtr = outputSamples)
            fixed (byte* bufferPtr = inputBuffer)
            {
                return opus_custom_decode_float(decoder, bufferPtr, inputBufferSize, samplesPtr, outputSamples.Length / Channels);
            }
        }

        public unsafe int Encode(short[] audioSamples, byte[] outputBuffer)
        {
            fixed (short* samplesPtr = audioSamples)
            fixed (byte* bufferPtr = outputBuffer)
            {
                return opus_custom_encode(encoder, samplesPtr, audioSamples.Length / Channels, bufferPtr, outputBuffer.Length);
            }
        }

        public unsafe int Encode(float[] audioSamples, byte[] outputBuffer)
        {
            fixed (float* samplesPtr = audioSamples)
            fixed (byte* bufferPtr = outputBuffer)
            {
                return opus_custom_encode_float(encoder, samplesPtr, audioSamples.Length / Channels, bufferPtr, outputBuffer.Length);
            }
        }

        public void ResetDecoder()
        {
            opus_custom_decoder_ctl_reset(decoder, OPUS_RESET_STATE);
        }

        public unsafe int GetDecoderSampleDelay()
        {
            // opus_custom_decoder_ctl is C-variadic and Mono on Apple rejects __arglist in
            // DllImport ("Vararg calling convention not supported"). stride_celt_get_lookahead
            // is a tiny non-variadic shim in libCelt.a (built from deps/Celt/celt_extras.c).
            // Re-evaluate this shim once Apple platforms move to CoreCLR — at that point a
            // pure-managed __arglist call should work and the shim can be retired.
            int delay = 0;
            int err = stride_celt_get_lookahead(decoder, &delay);
            return err == 0 ? delay : 0;
        }

        // libCelt opus_custom_* exports — see LibCelt constant above for per-platform resolution.
        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opus_custom_mode_create(int sampleRate, int frameSize, IntPtr error);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern void opus_custom_mode_destroy(IntPtr mode);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opus_custom_decoder_create(IntPtr mode, int channels, IntPtr error);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern void opus_custom_decoder_destroy(IntPtr decoder);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int opus_custom_decode(IntPtr decoder, byte* data, int len, short* pcm, int frameSize);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int opus_custom_decode_float(IntPtr decoder, byte* data, int len, float* pcm, int frameSize);

        // opus_custom_decoder_ctl is variadic. Calling it with no variadic args (RESET_STATE) is
        // safe under any ABI — nothing for va_arg to read. For requests that need an out-pointer
        // we go through stride_celt_get_lookahead (see celt_extras.c).
        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, EntryPoint = "opus_custom_decoder_ctl", CallingConvention = CallingConvention.Cdecl)]
        private static extern int opus_custom_decoder_ctl_reset(IntPtr decoder, int request);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int stride_celt_get_lookahead(IntPtr decoder, int* outDelay);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opus_custom_encoder_create(IntPtr mode, int channels, IntPtr error);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern void opus_custom_encoder_destroy(IntPtr encoder);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int opus_custom_encode(IntPtr encoder, short* pcm, int frameSize, byte* data, int maxDataBytes);

        [SuppressUnmanagedCodeSecurity, DllImport(LibCelt, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int opus_custom_encode_float(IntPtr encoder, float* pcm, int frameSize, byte* data, int maxDataBytes);
    }
}
#endif
