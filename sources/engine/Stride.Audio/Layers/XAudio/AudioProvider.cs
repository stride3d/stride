// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if WINDOWS

using System;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.XAudio;
using Stride.Audio.Layers.XAudio;
using Stride.Core.Mathematics;

namespace Stride.Audio
{
    internal sealed class AudioProvider
    {
        private const int AUDIO_CHANNELS = 2;
        private const float XAUDIO2_MAX_FREQ_RATIO = 1024.0f;
        private const float SPEED_OF_SOUND = 343.5f;
        private readonly Guid HrtfParamsIID = new("15B3CD66-E9DE-4464-B6E6-2BC3CF63D455");

        private XAudio xAudio;

        public AudioProvider()
		{
            xAudio = XAudio.GetApi();
        }

        public unsafe AudioBuffer BufferCreate(int maxBufferSizeBytes)
        {
            var buffer = new AudioBuffer();
            AudioBuffer* bufferPtr = &buffer;        
			buffer.Buffer.PContext = bufferPtr;
            
            var data = stackalloc byte[maxBufferSizeBytes];
			buffer.Buffer.PAudioData = data;
			return buffer;
        }

        public void BufferDestroy(AudioBuffer buffer)
        {
            //nothing to destroy (?)
        }

        public unsafe void BufferFill(AudioBuffer buffer, nint pcm, int bufferSize, int sampleRate, bool mono)
        {			
			buffer.Buffer.AudioBytes = (uint)bufferSize;
			
			buffer.Buffer.PlayBegin = 0;
            buffer.Size = bufferSize / sizeof(short) / (mono ? 1 : 2);
            buffer.Buffer.PlayLength = (uint)buffer.Size;

            buffer.Buffer.PAudioData = (byte*)pcm.ToPointer();
        }

        public unsafe Device? Create(string deviceName, DeviceFlags flags)
        {
            var device = new Device();

            device.hrtf = flags == DeviceFlags.Hrtf;

            //XAudio2, no flags, processor 1
            var result = xAudio.CreateWithVersionInfo(ref device.xAudio, device.hrtf ? 0x8000u : 0, 1, 0);
            if (HResult.IndicatesFailure(result))
            {
                return null;
            }

            //this means opening the real audio device, which will be virtual actually so in the case of default device change Xaudio will deal with it for us.
            result = device.xAudio->CreateMasteringVoice(ref device.masteringVoice, AUDIO_CHANNELS, 0, 0, deviceName, null, AudioStreamCategory.GameEffects);
            if (HResult.IndicatesFailure(result))
            {
                return null;
            }

            //start audio rendering
            result = device.xAudio->StartEngine();
            if (HResult.IndicatesFailure(result))
            {
                return null;
            }		
			
			//X3DAudio
			result = X3DAudio.X3DAudioInitialize(3, SPEED_OF_SOUND, out device.x3_audio);
			if (HResult.IndicatesFailure(result))
			{
				return null;
			}

			return device;
        }

        public unsafe void Destroy(Device device)
        {
            device.xAudio->StopEngine();
			
			device.masteringVoice->DestroyVoice();
        }

        public Listener ListenerCreate(Device device)
        {
            var listener = new Listener();
			listener.device = device;
            listener.listener = new();
            return listener;
        }

        public void ListenerDestroy(Listener listener)
        {
           //nothind to delete
        }

        public void ListenerDisable(Listener listener)
        {
            //unused in Xaudio2
        }

        public bool ListenerEnable(Listener listener)
        {
			return true;
        }

        public void ListenerPush3D(Listener listener, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
        {
            listener.listener.Position = pos;
            listener.listener.Velocity = vel;
            listener.listener.OrientFront = forward;
            listener.listener.OrientTop = up;
            listener.worldTransform = worldTransform;
        }

        public unsafe void SetMasterVolume(Device device, float volume)
        {
            device.masteringVoice->SetVolume(volume,0);
        }

        public unsafe Source SourceCreate(Listener listener, int sampleRate, int maxNumberOfBuffers, bool mono, bool spatialized, bool streamed, bool hrtf, float hrtfDirectionFactor, HrtfEnvironment environment)
        {
            var source = new Source
            {
                Listener = listener,
                SampleRate = sampleRate,
                Mono = mono,
                Streamed = streamed,
            };
			source.masteringVoice = listener.device.masteringVoice;
            
			if((spatialized && !hrtf) || (hrtf && !source.Listener.device.hrtf))
			{
				//if spatialized we also need those structures to calculate 3D audio
				source.emitter = new();
				source.emitter.ChannelCount = 1;
				source.emitter.CurveDistanceScaler = 1;
				source.emitter.DopplerScaler = 1;

				source.dsp_settings = new();
				source.dsp_settings.SrcChannelCount = 1;
				source.dsp_settings.DstChannelCount = AUDIO_CHANNELS;
                var matrix = stackalloc float[AUDIO_CHANNELS];
                source.dsp_settings.pMatrixCoefficients = matrix;
                var delay = stackalloc float[AUDIO_CHANNELS];
                source.dsp_settings.pDelayTimes = delay;
            }
            else
            {
                source.emitter = null;
				source.dsp_settings = null;
            }

			//we could have used a tinystl vector but it did not link properly on ARM windows... so we just use an array
			source.freeBuffers = new AudioBuffer[maxNumberOfBuffers];
			source.freeBuffersMax = maxNumberOfBuffers;
			for (int i = 0; i < maxNumberOfBuffers; i++)
			{
				source.freeBuffers[i] = null;
			}

			//Normal PCM formal 16 bit shorts
			WaveFormatEx pcmWaveFormat = new();
            pcmWaveFormat.WFormatTag = 1;
            pcmWaveFormat.NChannels = mono ? (ushort)1 : (ushort)2;
			pcmWaveFormat.NSamplesPerSec = (uint)sampleRate;
            pcmWaveFormat.NAvgBytesPerSec = (uint)(sampleRate * pcmWaveFormat.NChannels * sizeof(short));
            pcmWaveFormat.WBitsPerSample = 16;
			pcmWaveFormat.NBlockAlign = (ushort)(pcmWaveFormat.NChannels * pcmWaveFormat.WBitsPerSample / 8);


            int result = listener.device.xAudio->CreateSourceVoice(ref source.sourceVoice, &pcmWaveFormat, 0, XAUDIO2_MAX_FREQ_RATIO, null, null, null);
            if (HResult.IndicatesFailure(result))
            {
                return null;
            }

            if (spatialized && source.Listener.device.hrtf && hrtf)
            {
                IXAudio2SubmixVoice* submixVoice = null;

                HrtfDirectivity directivity = new(HrtfDirectivityType.OmniDirectional, hrtfDirectionFactor);
                HrtfApoInit apoInit = new(directivity);

                IUnknown apoRoot = new();
                result = HrtpApo.CreateHrtfApo(&apoInit, &apoRoot);
                if (HResult.IndicatesFailure(result))
                {
                    return null;
                }

                fixed (void* hrtfPtr = &source.hrtf_params) { 
					fixed(Guid* guidPtr = &HrtfParamsIID)
                		apoRoot.QueryInterface(guidPtr, &hrtfPtr);
				}

				EffectDescriptor fxDesc = new();
				fxDesc.InitialState = true;
				fxDesc.OutputChannels = 2; // Stereo output
				fxDesc.PEffect = (IUnknown*)source.hrtf_params; // HRTF xAPO set as the effect.

				EffectChain fxChain = new();
				fxChain.EffectCount = 1;
				fxChain.PEffectDescriptors = &fxDesc;

				VoiceSends sends = new();
				SendDescriptor sendDesc = new();
				sendDesc.POutputVoice = (IXAudio2Voice*)source.masteringVoice;
				sends.SendCount = 1;
				sends.PSends = &sendDesc;

				// HRTF APO expects mono 48kHz input, so we configure the submix voice for that format.
				result = listener.device.xAudio->CreateSubmixVoice(&submixVoice, 1, 48000, 0, 0, &sends, &fxChain);
				if (HResult.IndicatesFailure(result))
				{
					return null;
				}

				source.hrtf_params->SetEnvironment(environment);

				VoiceSends voice_sends = new();
                SendDescriptor voice_sendDesc = new();
                voice_sendDesc.POutputVoice = (IXAudio2Voice*)submixVoice;
				voice_sends.SendCount = 1;
				voice_sends.PSends = &voice_sendDesc;
				result = source.sourceVoice->SetOutputVoices(&voice_sends);
				if (HResult.IndicatesFailure(result))
				{
					return null;
				}
			}

			return source;
        }

        public unsafe void SourceDestroy(Source source)
        {
            source.sourceVoice->Stop(0,0);
			source.sourceVoice->DestroyVoice();
        }

        public unsafe void SourceFlushBuffers(Source source)
        {
            source.sourceVoice->FlushSourceBuffers();
        }

        public AudioBuffer? SourceGetFreeBuffer(Source source)
        {
			AudioBuffer buffer = null;
			for (int i = 0; i < source.freeBuffersMax; i++)
			{
				if (source.freeBuffers[i] != null)
				{
					buffer = source.freeBuffers[i];
					source.freeBuffers[i] = null;
					break;
				}
			}
			
			return buffer;
        }

        public unsafe float SourceGetPosition(Source source)
        {
            VoiceState state;
			source.GetState(&state);

			if (!source.Streamed)
				return (source.singleBuffer.PlayBegin + state.SamplesPlayed - (ulong)source.samplesAtBegin) / (float)source.SampleRate;
			
			//things work different for streamed sources, but anyway we simply subtract the snapshotted samples at begin of the stream ( could be the begin of the loop )
			return (state.SamplesPlayed - (ulong)source.samplesAtBegin) / (float)source.SampleRate;
        }

        public bool SourceIsPlaying(Source source)
        {
            return source.playing || source.pause;
        }

        public unsafe void SourcePause(Source source)
        {
            source.sourceVoice->Stop(0,0);
			source.playing = false;
			source.pause = true;
        }

        public unsafe void SourcePlay(Source source)
        {
            source.sourceVoice->Start(0,0);
			source.playing = true;

			if(!source.Streamed && !source.pause)
			{
                VoiceState state = new();
                source.GetState(&state);
				source.samplesAtBegin = (int)state.SamplesPlayed;
			}

			source.pause = false;
        }

        public unsafe void SourcePush3D(Source source, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
        {
            if(source.hrtf_params != null)
			{
                Matrix invListener = source.Listener.worldTransform;
                Matrix.Invert(ref invListener, out invListener);
                Matrix.Multiply(ref worldTransform, ref invListener, out var localTransform);

                HrtfPosition hrtfEmitterPos = new() { x = localTransform.M41, y = localTransform.M42, z = localTransform.M43 };
                source.hrtf_params->SetSourcePosition(ref hrtfEmitterPos);

				// //set orientation, relative to head, already computed c# side, todo c++ side
				HrtfOrientation hrtfEmitterRot = new(){ 
					element = [
					localTransform.M11, localTransform.M12, localTransform.M13,
					localTransform.M21, localTransform.M22, localTransform.M23,
					localTransform.M31, localTransform.M32, localTransform.M33 ]
				};
				source.hrtf_params->SetSourceOrientation(ref hrtfEmitterRot);
			}
			else if (source.emitter != null) 
			{
				source.emitter.Position = pos;
				source.emitter.Velocity = vel;
				source.emitter.OrientFront = forward;
				source.emitter.OrientTop = up;

				//everything is calculated by Xaudio for us
                fixed(X3DAudioListener* listenerPtr = &source.Listener.listener)
                {
                    X3DAudio.X3DAudioCalculate(out source.Listener.device.x3_audio, listenerPtr, source.emitter,
                        Calculate.Matrix | Calculate.Doppler | Calculate.LPF_Direct | Calculate.Reverb, out source.dsp_settings);
                }
				var voice = (IXAudio2Voice)(*source.masteringVoice);
				source.sourceVoice->SetOutputMatrix(&voice, 1, AUDIO_CHANNELS, source.dsp_settings.pMatrixCoefficients, 0);
				source.dopplerPitch = source.dsp_settings.DopplerFactor;
				source.sourceVoice->SetFrequencyRatio(source.dsp_settings.DopplerFactor * source.pitch, 0);
                FilterParameters filter_parameters = new(FilterType.LowPassFilter,
                    2.0f * MathF.Sin(MathF.PI / 6.0f * source.dsp_settings.LPFDirectCoefficient),
                    1.0f);
                source.sourceVoice->SetFilterParameters(ref filter_parameters, 0);
			}
        }

        public unsafe void SourceQueueBuffer(Source source, AudioBuffer buffer, nint pcm, int bufferSize, BufferType streamType)
        {
            //used only when streaming, to fill a buffer, often..
			source.Streamed = true;

			//flag the stream
			buffer.Buffer.Flags = streamType == BufferType.EndOfStream ? (uint)XAudio.EndOfStream : 0;
			buffer.Type = streamType;
			
			buffer.Size = bufferSize;
            buffer.Buffer.AudioBytes = (uint)buffer.Size;
            buffer.Buffer.PAudioData = (byte*)pcm.ToPointer();
            fixed (Silk.NET.XAudio.Buffer* bufferPtr = &buffer.Buffer)
            {
                source.sourceVoice->SubmitSourceBuffer(bufferPtr, null);
            }
        }

        public unsafe void SourceSetBuffer(Source source, AudioBuffer buffer)
        {
            //this function is called only when the audio source is actually fully cached in memory, so we deal only with the first buffer
            source.Streamed = false;
            source.freeBuffers[0] = buffer;
            source.singleBuffer = buffer.Buffer;

            fixed (Silk.NET.XAudio.Buffer* bufferPtr = &source.singleBuffer)
            {
                source.sourceVoice->SubmitSourceBuffer(bufferPtr, null);
            }
        }

        public unsafe void SourceSetGain(Source source, float gain)
        {
            source.sourceVoice->SetVolume(gain,0);
        }

        public unsafe void SourceSetLooping(Source source, bool looped)
        {
            source.looped = looped;

			if (!source.Streamed)
			{
				if (!source.looped)
				{
					source.singleBuffer.LoopBegin = 0;
					source.singleBuffer.LoopLength = 0;
					source.singleBuffer.LoopCount = 0;
					source.singleBuffer.Flags = XAudio.EndOfStream;
				}
				else
				{
					source.singleBuffer.LoopBegin = source.singleBuffer.PlayBegin;
					source.singleBuffer.LoopLength = source.singleBuffer.PlayLength;
                    source.singleBuffer.LoopCount = XAudio.LoopInfinite;
                    source.singleBuffer.Flags = 0;
				}

				source.sourceVoice->FlushSourceBuffers();
                fixed(Silk.NET.XAudio.Buffer* bufferPtr = &source.singleBuffer)
                {
                    source.sourceVoice->SubmitSourceBuffer(bufferPtr, null);
                }
            }
        }

        public unsafe void SourceSetPan(Source source, float pan)
        {
            if (source.Mono)
			{
				var panning = new float[2];
				if (pan < 0)
				{
					panning[0] = 1.0f;
					panning[1] = 1.0f + pan;
				}
				else
				{
					panning[0] = 1.0f - pan;
					panning[1] = 1.0f;
				}
                fixed(float* panningPtr = &panning[0]){
                    source.sourceVoice->SetOutputMatrix((IXAudio2Voice*)source.masteringVoice, 1, AUDIO_CHANNELS, panningPtr, 0);
                }
                
            }
			else
			{
				var panning = new float[4];
				if (pan < 0)
				{
					panning[0] = 1.0f;
					panning[1] = 0.0f;
					panning[2] = 0.0f;
					panning[3] = 1.0f + pan;
				}
				else
				{
					panning[0] = 1.0f - pan;
					panning[1] = 0.0f;
					panning[2] = 0.0f;
					panning[3] = 1.0f;
				}
                fixed(float* panningPtr = &panning[0]){
				    source.sourceVoice->SetOutputMatrix((IXAudio2Voice*)source.masteringVoice, 2, AUDIO_CHANNELS, panningPtr, 0);
                }
			}
        }

        public unsafe void SourceSetPitch(Source source, float pitch)
        {
            source.pitch = pitch;
			source.sourceVoice->SetFrequencyRatio(source.dopplerPitch * source.pitch, 0);
        }

        public unsafe void SourceSetRange(Source source, double startTime, double stopTime)
        {
            if(!source.Streamed)
			{
				var singleBuffer = source.freeBuffers[0];
				if(startTime == 0 && stopTime == 0)
				{
					source.singleBuffer.PlayBegin = 0;
					source.singleBuffer.PlayLength = (uint)singleBuffer.Size;
				}
				else
				{					
					var sampleStart = (int)(source.SampleRate * startTime);
					var sampleStop = (int)(source.SampleRate * stopTime);

					if (sampleStart > singleBuffer.Size)
					{
						return; //the starting position must be less then the total length of the buffer
					}

					if (sampleStop > singleBuffer.Size) //if the end point is more then the length of the buffer fix the value
					{
						sampleStop = singleBuffer.Size;
					}

					var len = sampleStop - sampleStart;
					if (len > 0)
					{
						source.singleBuffer.PlayBegin = (uint)sampleStart;
						source.singleBuffer.PlayLength = (uint)len;
					}
				}

				//sort looping properties and re-submit buffer
				source.sourceVoice->Stop(0,0);
				SourceSetLooping(source, source.looped);
			}
        }

        public unsafe void SourceStop(Source source)
        {
            source.sourceVoice->Stop(0, 0);
			source.sourceVoice->FlushSourceBuffers();
			source.playing = false;
			source.pause = false;

			//since we flush we also rebuffer in this case
			if (!source.Streamed)
			{
                fixed (Silk.NET.XAudio.Buffer* bufferPtr = &source.singleBuffer)
                {
                    source.sourceVoice->SubmitSourceBuffer(bufferPtr, null);
                }
            }

        }

        public void Update(Device device)
        { 
        }
    }

    internal class HrtpApo
    {
        [DllImport("HrtfApo")]
        public static extern unsafe int CreateHrtfApo(HrtfApoInit* v1, IUnknown* v2);
    }

    internal class X3DAudio
    {
        [DllImport("XAudio2_9")]
        public static extern int X3DAudioInitialize(uint SpeakerChannelMask, float SpeedOfSound, out nint Instance);

        [DllImport("XAudio2_9")]
        public static extern unsafe void X3DAudioCalculate(out nint x3_audio, X3DAudioListener* listenerPtr, X3DAudioEmitter emitter, Calculate calculate, out X3DAudioDSPSettings dsp_settings);
    }
}
#endif
