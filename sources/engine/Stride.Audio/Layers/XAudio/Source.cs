// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if WINDOWS
using Silk.NET.XAudio;
using Stride.Audio.Layers.XAudio;

namespace Stride.Audio;

public sealed unsafe partial class Source
{
    public IXAudio2VoiceCallback value = new();
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

    public unsafe void GetState(VoiceState* state)
    {
        sourceVoice->GetState(state, 0);
    }
}
#endif
