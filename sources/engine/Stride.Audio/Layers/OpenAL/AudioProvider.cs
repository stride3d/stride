// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if LINUX || OSX 
using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenAL;
using Stride.Core.Mathematics;

namespace Stride.Audio;

internal sealed unsafe class AudioProvider
{
    private readonly ALContext alc;
    private readonly AL al;
    public AudioProvider()
    {
        alc = ALContext.GetApi();
        al = AL.GetApi();
    }

    public AudioBuffer BufferCreate(int maxBufferSize)
    {
        var buffer = new AudioBuffer();
        buffer.Pcm = new short[maxBufferSize];
        al.GenBuffers(1, &buffer.Value);
        buffer.Initialized = true;
        return buffer;
    }

    public void BufferDestroy(AudioBuffer buffer)
    {
        al.DeleteBuffers(1, &buffer.Value);
        buffer.Initialized = false;
	    buffer.Pcm = null;
    }

    public void BufferFill(AudioBuffer buffer, nint pcm, int bufferSize, int sampleRate, bool mono)
    {
        //we have to keep a copy sadly because we might need to offset the data at some point	

        buffer.Pcm = new short[bufferSize];		
		Marshal.Copy(pcm, buffer.Pcm, 0, bufferSize);

        buffer.Size = bufferSize;
        buffer.SampleRate = sampleRate;
			
		al.BufferData(buffer.Value, mono ? BufferFormat.Mono16 : BufferFormat.Stereo16, pcm.ToPointer(), bufferSize, sampleRate);
    }

    public void SourceSetPan(Source source, float pan)
    {
        float clampedPan = pan > 1.0f ? 1.0f : pan < -1.0f ? -1.0f : pan;
        _ = new ContextState(source.Listener.Context);

        al.SetSourceProperty(source.Value, SourceVector3.Position, clampedPan, MathF.Sqrt(1.0f - clampedPan * clampedPan), 0f);
    }

    public Listener ListenerCreate(Device device)
    {
        var listener = new Listener();
		listener.Device = device;

		listener.Context = alc.CreateContext(device.Value, null);
        listener.Initialized = alc.GetError(device.Value) == ContextError.NoError;
        alc.MakeContextCurrent(listener.Context);
        listener.Initialized = alc.GetError(device.Value) == ContextError.NoError;
        alc.ProcessContext(listener.Context);        
        listener.Initialized = alc.GetError(device.Value) == ContextError.NoError;
        
        device.DeviceLock.Lock();

        device.Listeners.Add(listener);

		device.DeviceLock.Unlock();

		return listener;
    }

    public void ListenerDestroy(Listener listener)
    {
        listener.Device.DeviceLock.Lock();

        listener.Device.Listeners.Remove(listener);

        listener.Device.DeviceLock.Unlock();

        listener.Initialized = false;

        alc.DestroyContext(listener.Context);
    }

    public void ListenerDisable(Listener listener)
    {
        alc.SuspendContext(listener.Context);
		alc.MakeContextCurrent(null);
    }

    public bool ListenerEnable(Listener listener)
    {
        bool res = alc.MakeContextCurrent(listener.Context);
		alc.ProcessContext(listener.Context);
		return res;
    }

    public void ListenerPush3D(Listener listener, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
    {
        _ = new ContextState(listener.Context);

        float[] ori = [forward[0], forward[1], -forward[2], up[0], up[1], -up[2]];

        fixed(float * pOri = ori)
        {
            al.SetListenerProperty(ListenerFloatArray.Orientation, pOri);
        }
        al.SetListenerProperty(ListenerVector3.Position, pos.X, pos.Y, pos.Z);
    
        al.SetListenerProperty(ListenerVector3.Velocity, vel.X, vel.Y, -vel.Z);			
    }

    public Source SourceCreate(Listener listener, int sampleRate, int maxNumberOfBuffers, bool mono, bool spatialized, bool streamed, bool hrtf, float hrtfDirectionFactor, HrtfEnvironment environment)
    {
        var source = new Source
        {
            Listener = listener,
            SampleRate = sampleRate,
            Mono = mono,
            Streamed = streamed,
        };
        _ = new ContextState(listener.Context);

        al.GenSources(1, &source.Sources);
        al.GenSources(1, &source.Value);

        source.Initialized = al.GetError() == AudioError.NoError;
        al.SetSourceProperty(source.Value, SourceFloat.ReferenceDistance, 1.0f);
        source.Initialized = al.GetError() == AudioError.NoError;

        if(spatialized)
        {
            //make sure we are able to 3D
            al.SetSourceProperty(source.Value, SourceBoolean.SourceRelative, false);
        }
        else
        {
            //make sure we are able to pan
            al.SetSourceProperty(source.Value, SourceBoolean.SourceRelative, true);
        }

        listener.Sources.Add(source);
        
        return source;
    }

    public void SourceDestroy(Source source)
    {
        _ = new ContextState(source.Listener.Context);

        al.DeleteSources(1, &source.Value);
        source.Initialized = false;
        source.Listener.Sources.Remove(source);
    }

    public void SourceFlushBuffers(Source source)
    {
        _ = new ContextState(source.Listener.Context);

        if (source.Streamed)
        {
            //flush all buffers
            al.GetSourceProperty(source.Value, GetSourceInteger.BuffersProcessed, out var processed);
            while (processed-- <= 0)
            {
                uint buffer = 0;
                al.SourceUnqueueBuffers(source.Value, 1, &buffer);
            }

            //return the source to undetermined mode
            al.SetSourceProperty(source.Value, SourceInteger.Buffer, 0);

            //set all buffers as free
            source.FreeBuffers.Clear();
            foreach (var buffer in source.Listener.Buffers)
            {
                source.FreeBuffers.Add(buffer.Value);
            }
		}
    }

    public AudioBuffer? SourceGetFreeBuffer(Source source)
    {
        _ = new ContextState(source.Listener.Context);

        if(source.FreeBuffers.Count > 0)
        {
            var buffer = source.FreeBuffers[^1];
            source.FreeBuffers.Remove(buffer);
            return buffer;
        }

		return null;
    }

    public float SourceGetPosition(Source source)
    {
        _ = new ContextState(source.Listener.Context);

        al.GetSourceProperty(source.Value, SourceFloat.SecOffset, out var offset);

        if (!source.Streamed)
        {				
            return offset;
        }

        return offset + source.DequeuedTime;
    }

    public bool SourceIsPlaying(Source source)
    {
        _ = new ContextState(source.Listener.Context);
		al.GetSourceProperty(source.Value, GetSourceInteger.SourceState, out var value);
		return value == (int)SourceState.Paused || value == (int)SourceState.Paused;
    }

    public void SourcePause(Source source)
    {
        _ = new ContextState(source.Listener.Context);
        al.SourcePause(source.Value);
    }

    public void SourcePlay(Source source)
    {
        _ = new ContextState(source.Listener.Context);
        al.SourcePlay(source.Value);
    }

    public void SourcePush3D(Source source, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
    {
        _ = new ContextState(source.Listener.Context);

        float[] ori = [forward[0], forward[1], -forward[2], up[0], up[1], -up[2]];

        fixed(float * pOri = ori)
        {
            al.SetSourceProperty(source.Value, SourceVector3.Direction, pOri);//Todo maybe I should add vector here
        }
        al.SetSourceProperty(source.Value, SourceVector3.Position, pos.X, pos.Y, pos.Z);
    
        al.SetSourceProperty(source.Value, SourceVector3.Velocity, vel.X, vel.Y, -vel.Z);	
    }

    public void SourceQueueBuffer(Source source, AudioBuffer buffer, nint pcm, int bufferSize, BufferType streamType)
    {
        _ = new ContextState(source.Listener.Context);
        
        buffer.Type = streamType;
        buffer.Size = bufferSize;
        al.BufferData(buffer.Value, source.Mono ? BufferFormat.Mono16 : BufferFormat.Stereo16, pcm.ToPointer(), bufferSize, source.SampleRate);
        al.SourceQueueBuffers(source.Value, 1, &buffer.Value);
        source.Listener.Buffers[buffer.Value] = buffer;
    }

    public void SourceSetBuffer(Source source, AudioBuffer buffer)
    {
        _ = new ContextState(source.Listener.Context);
        source.SingleBuffer = buffer;
		al.SetSourceProperty(source.Value, SourceInteger.Buffer, buffer.Value);
    }

    public void SourceSetGain(Source source, float gain)
    {
        _ = new ContextState(source.Listener.Context);
        al.SetSourceProperty(source.Value, SourceFloat.Gain, gain);
    }

    public void SourceSetLooping(Source source, bool looped)
    {
        _ = new ContextState(source.Listener.Context);
        al.SetSourceProperty(source.Value, SourceBoolean.Looping, looped);
    }

    public void SourceSetPitch(Source source, float pitch)
    {
        _ = new ContextState(source.Listener.Context);
        al.SetSourceProperty(source.Value, SourceFloat.Pitch, pitch);
    }

    public void SourceSetRange(Source source, double startTime, double stopTime)
    {
        if (source.Streamed)
        {
            return;
        }

        _ = new ContextState(source.Listener.Context);

        al.GetSourceProperty(source.Value, GetSourceInteger.SourceState, out var playing);
        if (playing == (int)SourceState.Playing) 
            al.SourceStop(source.Value);

        al.SetSourceProperty(source.Value, SourceInteger.Buffer, 0);

        //OpenAL is kinda bad and offers only starting offset...
        //As result we need to rewrite the buffer
        if(startTime == 0 && stopTime == 0)
        {
            //cancel the offsetting			
            fixed (short* pPcm = source.SingleBuffer.Pcm)
            {				
                al.BufferData(source.SingleBuffer.Value, source.Mono ? BufferFormat.Mono16 : BufferFormat.Stereo16, pPcm, source.SingleBuffer.Size, source.SingleBuffer.SampleRate);	
            }					
        }
        else
        {
            //offset the data
            int sampleStart = (int)(source.SingleBuffer.SampleRate * (source.Mono ? 1.0 : 2.0) * startTime);
            int sampleStop = (int)(source.SingleBuffer.SampleRate * (source.Mono ? 1.0 : 2.0) * stopTime);

            if (sampleStart > source.SingleBuffer.Size / sizeof(short))
            {
                return; //the starting position must be less then the total length of the buffer
            }

            if (sampleStop > source.SingleBuffer.Size / sizeof(short)) //if the end point is more then the length of the buffer fix the value
            {
                sampleStop = source.SingleBuffer.Size / sizeof(short);
            }

            var len = sampleStop - sampleStart;


            fixed (short* pPcm = source.SingleBuffer.Pcm)
            {            
                short* offsettedBuffer = pPcm + sampleStart;
                al.BufferData(source.SingleBuffer.Value, source.Mono ? BufferFormat.Mono16 : BufferFormat.Stereo16, offsettedBuffer, len * sizeof(short), source.SingleBuffer.SampleRate);
            }
        }

        al.SetSourceProperty(source.Value, SourceInteger.Buffer, source.SingleBuffer.Value);
        if (playing == (int)SourceState.Playing) 
            al.SourcePlay(source.Value);
    }

    public void SourceStop(Source source)
    {
        _ = new ContextState(source.Listener.Context);

        al.SourceStop(source.Value);
		SourceFlushBuffers(source);

        //reset timing info
        if(source.Streamed)
            source.DequeuedTime = 0.0f;
    }

    public Device? Create(string deviceName, DeviceFlags flags)
    {
        var device = new Device();
        device.Value = alc.OpenDevice(deviceName);
		alc.GetError(device.Value);
        if (device.Value == null)
        {
            return null;
        }
        return device;
    }

    public void Destroy(Device device)
    {
        alc.CloseDevice(device.Value);
    }

    public void SetMasterVolume(Device device, float volume)
    {
        device.DeviceLock.Lock();
        for (var i = 0; i < device.Listeners.Count; i++)
        {
            var listener = device.Listeners[i];
            _ = new ContextState(listener.Context);
            al.SetListenerProperty(ListenerFloat.Gain, volume);
        }
        device.DeviceLock.Unlock();
    }

    public void Update(Device device)
    {
        device.DeviceLock.Lock();

        for (var i = 0; i < device.Listeners.Count; i++)
        {
            var listener = device.Listeners[i];
            _ = new ContextState(listener.Context);

            for (var j = 0; j < listener.Sources.Count; j++)
            {
                var source = listener.Sources[j];
                if (source.Streamed)
                {
                    int processed = 0;
                    al.GetSourceProperty(source.Value, GetSourceInteger.BuffersProcessed, &processed);
                    while (processed-- > 0)
                    {
                        float preDTime;
                        al.GetSourceProperty(source.Value, SourceFloat.SecOffset, &preDTime);

                        uint buffer;
                        al.SourceUnqueueBuffers(source.Value, 1, &buffer);
                        var bufferPtr = source.Listener.Buffers[buffer];

                        float postDTime;
                        al.GetSourceProperty(source.Value, SourceFloat.SecOffset, &postDTime);

                        if (bufferPtr.Type == BufferType.EndOfStream || bufferPtr.Type == BufferType.EndOfLoop)
                        {
                            source.DequeuedTime = 0.0f;
                        }
                        else
                        {
                            source.DequeuedTime += preDTime - postDTime;
                        }

                        source.FreeBuffers.Add(bufferPtr);
                    }
                }
            }
        }
        device.DeviceLock.Unlock();
    }
}
#endif