// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if WINDOWS
using System.Runtime.InteropServices;
using Silk.NET.XAudio;
using Stride.Audio.Layers.XAudio;
using Buffer = Silk.NET.XAudio.Buffer;

namespace Stride.Audio;

public sealed unsafe partial class Source
{
    public IXAudio2MasteringVoice* masteringVoice;
    public IXAudio2SourceVoice* sourceVoice;
    public X3DAudioEmitter emitter;
    public X3DAudioDSPSettings dsp_settings;
    public IXAPOHrtfParameters* hrtf_params;
    internal Listener Listener;
    public volatile bool playing;
    public volatile bool pause;
    public volatile bool looped;
    public volatile float pitch = 1.0f;
    public volatile float dopplerPitch = 1.0f;
    internal AudioBuffer[] freeBuffers;
    public int freeBuffersMax;

    public Buffer singleBuffer;

    public volatile int samplesAtBegin = 0; 

    public void GetState(VoiceState* state)
    {
        sourceVoice->GetState(state, 0);
    }
    
    public void OnVoiceProcessingPassStart(uint BytesRequired) {}
    public void OnVoiceProcessingPassEnd() {}
    
    public void OnStreamEnd()
    {
        if (playing)
        {
            if (Streamed)
            {
                //buffer was flagged as end of stream
                //looping is handled by the streamer, in the top level layer
                new AudioProvider().SourceStop(this);
            }
            else if (!looped)
            {
                playing = false;
                pause = false;
            }
        }
    }

    public void OnBufferStart(void* pBufferContext)
    {
        if (Streamed)
        {
            var buffer = Marshal.PtrToStructure<AudioBuffer>(new(pBufferContext));

            if (buffer.Type == BufferType.BeginOfStream)
            {
                //we need this info to compute position of stream
                VoiceState state;
                GetState(&state);

                samplesAtBegin = (int)state.SamplesPlayed;
            }
        }
    }
    
    public void OnBufferEnd(void* pBufferContext)
    {
        if (Streamed)
        {
            var buffer = Marshal.PtrToStructure<AudioBuffer>(new(pBufferContext));

            for (int i = 0; i < freeBuffersMax; i++)
            {
                if (freeBuffers[i] == null)
                {
                    freeBuffers[i] = buffer;
                    break;
                }
            }

        }	
    }
    
    public void OnLoopEnd(void* pBufferContext)
    {
        if (!Streamed)
        {
            VoiceState state;
            GetState(&state);

            samplesAtBegin = (int)state.SamplesPlayed;
        }
    }
    
    public void OnVoiceError(void* pBufferContext, int Error) { }
}
#endif
