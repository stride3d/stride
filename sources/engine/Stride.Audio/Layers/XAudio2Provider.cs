// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Audio
{
    internal class XAudio2Provider : IAudioProvider
    {
        public XAudio2Provider()
        {
        }

        public AudioBuffer BufferCreate(int maxBufferSizeBytes)
        {
            throw new System.NotImplementedException();
        }

        public void BufferDestroy(AudioBuffer buffer)
        {
            throw new System.NotImplementedException();
        }

        public void BufferFill(AudioBuffer buffer, nint pcm, int bufferSize, int sampleRate, bool mono)
        {
            throw new System.NotImplementedException();
        }

        public Device? Create(string deviceName, DeviceFlags flags)
        {
            throw new System.NotImplementedException();
        }

        public void Destroy(Device device)
        {
            throw new System.NotImplementedException();
        }

        public Listener ListenerCreate(Device device)
        {
            throw new System.NotImplementedException();
        }

        public void ListenerDestroy(Listener listener)
        {
            throw new System.NotImplementedException();
        }

        public void ListenerDisable(Listener listener)
        {
            throw new System.NotImplementedException();
        }

        public bool ListenerEnable(Listener listener)
        {
            throw new System.NotImplementedException();
        }

        public void ListenerPush3D(Listener listener, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
        {
            throw new System.NotImplementedException();
        }

        public void SetMasterVolume(Device device, float volume)
        {
            throw new System.NotImplementedException();
        }

        public Source SourceCreate(Listener listener, int sampleRate, int maxNumberOfBuffers, bool mono, bool spatialized, bool streamed, bool hrtf, float hrtfDirectionFactor, HrtfEnvironment environment)
        {
            throw new System.NotImplementedException();
        }

        public void SourceDestroy(Source source)
        {
            throw new System.NotImplementedException();
        }

        public void SourceFlushBuffers(Source source)
        {
            throw new System.NotImplementedException();
        }

        public AudioBuffer? SourceGetFreeBuffer(Source source)
        {
            throw new System.NotImplementedException();
        }

        public float SourceGetPosition(Source source)
        {
            throw new System.NotImplementedException();
        }

        public bool SourceIsPlaying(Source source)
        {
            throw new System.NotImplementedException();
        }

        public void SourcePause(Source source)
        {
            throw new System.NotImplementedException();
        }

        public void SourcePlay(Source source)
        {
            throw new System.NotImplementedException();
        }

        public void SourcePush3D(Source source, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
        {
            throw new System.NotImplementedException();
        }

        public void SourceQueueBuffer(Source source, AudioBuffer buffer, nint pcm, int bufferSize, BufferType streamType)
        {
            throw new System.NotImplementedException();
        }

        public void SourceSetBuffer(Source source, AudioBuffer buffer)
        {
            throw new System.NotImplementedException();
        }

        public void SourceSetGain(Source source, float gain)
        {
            throw new System.NotImplementedException();
        }

        public void SourceSetLooping(Source source, bool looped)
        {
            throw new System.NotImplementedException();
        }

        public void SourceSetPan(Source source, float pan)
        {
            throw new System.NotImplementedException();
        }

        public void SourceSetPitch(Source source, float pitch)
        {
            throw new System.NotImplementedException();
        }

        public void SourceSetRange(Source source, double startTime, double stopTime)
        {
            throw new System.NotImplementedException();
        }

        public void SourceStop(Source source)
        {
            throw new System.NotImplementedException();
        }

        public void Update(Device device)
        {
            throw new System.NotImplementedException();
        }
    }
}