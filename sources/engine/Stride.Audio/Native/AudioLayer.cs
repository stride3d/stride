// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;

namespace Stride.Audio;

/// <summary>
/// Wrapper around Audio provider like OpenAL
/// </summary>
public class AudioLayer
{
    private static IAudioProvider al;

    static AudioLayer()
    {
        NativeInvoke.PreLoad();
    }

    public static void Init()
    {
        al = new OpenALProvider();
    }

    public static Device? Create(string deviceName, DeviceFlags flags)
    {
        return al.Create(deviceName, flags);
    }

    public static void Destroy(Device device)
    {
        al.Destroy(device);
    }

    public static void Update(Device device)
    {
        al.Update(device);
    }

    public static void SetMasterVolume(Device device, float volume)
    {
        al.SetMasterVolume(device,volume);
    }

    public static Listener ListenerCreate(Device device)
    {
        return al.ListenerCreate(device);
    }

    public static void ListenerDestroy(Listener listener)
    {
        al.ListenerDestroy(listener);
    }

    public static bool ListenerEnable(Listener listener)
    {
        return al.ListenerEnable(listener);
    }

    public static void ListenerDisable(Listener listener)
    {
        al.ListenerDisable(listener);
    }

    public static Source SourceCreate(Listener listener, int sampleRate, int maxNumberOfBuffers, bool mono, bool spatialized, bool streamed, bool hrtf, float hrtfDirectionFactor, HrtfEnvironment environment)
    {
        return al.SourceCreate(listener, sampleRate, maxNumberOfBuffers, mono, spatialized, streamed, hrtf, hrtfDirectionFactor, environment);
    }

    public static void SourceDestroy(Source source)
    {
        al.SourceDestroy(source);
    }

    public static float SourceGetPosition(Source source)
    {
        return al.SourceGetPosition(source);
    }

    public static void SourceSetPan(Source source, float pan)
    {
        al.SourceSetPan(source, pan);
    }

    public static AudioBuffer BufferCreate(int maxBufferSizeBytes)
    {
        return al.BufferCreate(maxBufferSizeBytes);
    }

    public static void BufferDestroy(AudioBuffer buffer)
    {
        al.BufferDestroy(buffer);
    }

    public static void BufferFill(AudioBuffer buffer, IntPtr pcm, int bufferSize, int sampleRate, bool mono)
    {
        al.BufferFill(buffer, pcm, bufferSize, sampleRate, mono);
    }

    public static void SourceSetBuffer(Source source, AudioBuffer buffer)
    {
        al.SourceSetBuffer(source, buffer);
    }

    public static void SourceFlushBuffers(Source source)
    {
        al.SourceFlushBuffers(source);
    }

    public static void SourceQueueBuffer(Source source, AudioBuffer buffer, IntPtr pcm, int bufferSize, BufferType streamType)
    {
        al.SourceQueueBuffer(source, buffer, pcm, bufferSize, streamType);
    }

    public static AudioBuffer? SourceGetFreeBuffer(Source source)
    {
        return al.SourceGetFreeBuffer(source);
    }

    public static void SourcePlay(Source source)
    {
        al.SourcePlay(source);
    }

    public static void SourcePause(Source source)
    {
        SourcePause(source);
    }

    public static void SourceStop(Source source)
    {
        al.SourceStop(source);
    }

    public static void SourceSetLooping(Source source, bool looped)
    {
        al.SourceSetLooping(source,looped);
    }

    public static void SourceSetRange(Source source, double startTime, double stopTime)
    {
        al.SourceSetRange(source, startTime, stopTime);
    }

    public static void SourceSetGain(Source source, float gain)
    {
        al.SourceSetGain(source, gain);
    }

    public static void SourceSetPitch(Source source, float pitch)
    {
        al.SourceSetPitch(source, pitch);
    }

    public static void ListenerPush3D(Listener listener, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
    {
        al.ListenerPush3D(listener, ref pos, ref forward, ref up, ref vel, ref worldTransform);
    }

    public static void SourcePush3D(Source source, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
    {
        al.SourcePush3D(source, ref pos, ref forward, ref up, ref vel, ref worldTransform);
    }

    public static bool SourceIsPlaying(Source source)
    {
        return al.SourceIsPlaying(source);
    }
}
