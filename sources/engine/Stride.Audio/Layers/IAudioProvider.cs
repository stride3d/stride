// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Audio;

internal interface IAudioProvider
{
    AudioBuffer BufferCreate(int maxBufferSizeBytes);
    void BufferDestroy(AudioBuffer buffer);
    void BufferFill(AudioBuffer buffer, nint pcm, int bufferSize, int sampleRate, bool mono);
    Device? Create(string deviceName, DeviceFlags flags);
    void SourceSetPan(Source source, float pan);
    void Destroy(Device device);
    Listener ListenerCreate(Device device);
    void ListenerDestroy(Listener listener);
    void ListenerDisable(Listener listener);
    bool ListenerEnable(Listener listener);
    void ListenerPush3D(Listener listener, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform);
    void SetMasterVolume(Device device, float volume);
    Source SourceCreate(Listener listener, int sampleRate, int maxNumberOfBuffers, bool mono, bool spatialized, bool streamed, bool hrtf, float hrtfDirectionFactor, HrtfEnvironment environment);
    void SourceDestroy(Source source);
    void SourceFlushBuffers(Source source);
    AudioBuffer? SourceGetFreeBuffer(Source source);
    float SourceGetPosition(Source source);
    bool SourceIsPlaying(Source source);
    void SourcePause(Source source);
    void SourcePlay(Source source);
    void SourcePush3D(Source source, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform);
    void SourceQueueBuffer(Source source, AudioBuffer buffer, nint pcm, int bufferSize, BufferType streamType);
    void SourceSetBuffer(Source source, AudioBuffer buffer);
    void SourceSetGain(Source source, float gain);
    void SourceSetLooping(Source source, bool looped);
    void SourceSetPitch(Source source, float pitch);
    void SourceSetRange(Source source, double startTime, double stopTime);
    void SourceStop(Source source);
    void Update(Device device);
}