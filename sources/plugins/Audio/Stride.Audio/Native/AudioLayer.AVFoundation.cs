// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_MACOS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AVFoundation;
using Foundation;
using Stride.Core.Mathematics;

namespace Stride.Audio
{
    /// <summary>
    /// AVAudioEngine-backed managed implementation of <see cref="AudioLayer"/>. Replaces
    /// libstrideaudio's OpenAL backend on Apple platforms. Handles flow through GCHandle: each
    /// public Device/Listener/Source/Buffer.Ptr wraps a GCHandle to a <see cref="ManagedDevice"/>,
    /// <see cref="ManagedListener"/>, etc., resolved with <see cref="ResolveHandle"/>.
    /// </summary>
    public partial class AudioLayer
    {
        // -- Handle plumbing -----------------------------------------------------

        private static IntPtr AllocHandle<T>(T obj) where T : class
        {
            return GCHandle.ToIntPtr(GCHandle.Alloc(obj, GCHandleType.Normal));
        }

        private static T ResolveHandle<T>(IntPtr ptr) where T : class
        {
            if (ptr == IntPtr.Zero) return null;
            var handle = GCHandle.FromIntPtr(ptr);
            return handle.Target as T;
        }

        private static void FreeHandle(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return;
            GCHandle.FromIntPtr(ptr).Free();
        }

        // -- Managed wrappers ----------------------------------------------------

        private sealed class ManagedDevice
        {
            public AVAudioEngine Engine;
            public AVAudioMixerNode Mixer;          // == Engine.MainMixerNode; cached for master volume
            public AVAudioEnvironmentNode Env;      // 3D mixer; spatialized sources connect here
            public List<ManagedSource> Sources = new();
        }

        private sealed class ManagedListener
        {
            public ManagedDevice Device;
            public bool Enabled;
        }

        private sealed class ManagedBuffer
        {
            public int CapacityBytes;               // max bytes BufferFill can write
            public AVAudioPcmBuffer Pcm;            // null until BufferFill is called the first time
            public AudioLayer.BufferType StreamType;
            // Owner's current schedule state — used by the streaming path to know when a buffer
            // becomes free again after a Source consumed it.
            public bool Scheduled;
        }

        private sealed class ManagedSource
        {
            public ManagedDevice Device;
            public ManagedListener Listener;
            public AVAudioPlayerNode Player;
            public AVAudioFormat Format;            // declared input format (int16 mono/stereo)
            public int SampleRate;
            public bool Mono;
            public bool Spatialized;
            public bool Streamed;
            public bool Looping;
            public float Gain = 1f;
            public ManagedBuffer StaticBuffer;      // SourceSetBuffer target; replays on Play
            public Queue<ManagedBuffer> StreamingFree = new();  // pre-allocated, returned via SourceGetFreeBuffer
            public Queue<ManagedBuffer> StreamingPending = new(); // queued, not yet consumed
            public double PlayRangeStart, PlayRangeStop;
        }

        // -- Init / Device -------------------------------------------------------

        public static bool Init()
        {
            return true;
        }

        public static Device Create(string deviceName, DeviceFlags flags)
        {
            try
            {
                var dev = new ManagedDevice
                {
                    Engine = new AVAudioEngine(),
                };
                dev.Mixer = dev.Engine.MainMixerNode;
                // EnvironmentNode is set up lazily on first spatialized SourceCreate. AVAudioEngine
                // on macOS deadlocks during Start if AttachNode/Connect happen before MainMixerNode
                // has been touched and the output node format is fully resolved.
                if (!dev.Engine.StartAndReturnError(out var error) || error != null)
                {
                    AudioEngine.Logger?.Warning($"AVAudioEngine.Start failed: {error?.LocalizedDescription}");
                    return default;
                }
                return new Device { Ptr = AllocHandle(dev) };
            }
            catch (Exception ex)
            {
                AudioEngine.Logger?.Error($"AudioLayer.Create threw: {ex}");
                return default;
            }
        }

        private static void EnsureEnv(ManagedDevice dev)
        {
            if (dev.Env != null) return;
            dev.Env = new AVAudioEnvironmentNode();
            dev.Engine.AttachNode(dev.Env);
            dev.Engine.Connect(dev.Env, dev.Mixer, null);
        }

        public static void Destroy(Device device)
        {
            var dev = ResolveHandle<ManagedDevice>(device.Ptr);
            if (dev == null) return;

            // Stop and tear down player nodes still attached.
            foreach (var src in dev.Sources)
            {
                try { src.Player?.Stop(); } catch { }
                try { if (src.Player != null) dev.Engine.DetachNode(src.Player); } catch { }
            }
            dev.Sources.Clear();

            try { dev.Engine.Stop(); } catch { }
            try { if (dev.Env != null) dev.Engine.DetachNode(dev.Env); } catch { }
            dev.Engine.Dispose();
            FreeHandle(device.Ptr);
        }

        public static void Update(Device device)
        {
            // AVAudioEngine is push-driven; no per-frame work required.
        }

        public static void SetMasterVolume(Device device, float volume)
        {
            var dev = ResolveHandle<ManagedDevice>(device.Ptr);
            if (dev?.Mixer == null) return;
            dev.Mixer.OutputVolume = volume;
        }

        // -- Listener ------------------------------------------------------------

        public static Listener ListenerCreate(Device device)
        {
            var dev = ResolveHandle<ManagedDevice>(device.Ptr);
            if (dev == null) return default;
            var listener = new ManagedListener { Device = dev };
            return new Listener { Ptr = AllocHandle(listener) };
        }

        public static void ListenerDestroy(Listener listener) => FreeHandle(listener.Ptr);

        public static bool ListenerEnable(Listener listener)
        {
            var l = ResolveHandle<ManagedListener>(listener.Ptr);
            if (l == null) return false;
            l.Enabled = true;
            return true;
        }

        public static void ListenerDisable(Listener listener)
        {
            var l = ResolveHandle<ManagedListener>(listener.Ptr);
            if (l != null) l.Enabled = false;
        }

        public static void ListenerPush3D(Listener listener, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
        {
            var l = ResolveHandle<ManagedListener>(listener.Ptr);
            if (l?.Device?.Env == null) return;
            l.Device.Env.ListenerPosition = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);
            l.Device.Env.ListenerVectorOrientation = new AVAudio3DVectorOrientation(
                new System.Numerics.Vector3(forward.X, forward.Y, forward.Z),
                new System.Numerics.Vector3(up.X, up.Y, up.Z));
        }

        // -- Buffer --------------------------------------------------------------

        public static Buffer BufferCreate(int maxBufferSizeBytes)
        {
            var buf = new ManagedBuffer { CapacityBytes = maxBufferSizeBytes };
            return new Buffer { Ptr = AllocHandle(buf) };
        }

        public static void BufferDestroy(Buffer buffer)
        {
            var b = ResolveHandle<ManagedBuffer>(buffer.Ptr);
            if (b != null)
            {
                b.Pcm?.Dispose();
                b.Pcm = null;
            }
            FreeHandle(buffer.Ptr);
        }

        public static unsafe void BufferFill(Buffer buffer, IntPtr pcm, int bufferSize, int sampleRate, bool mono)
        {
            var b = ResolveHandle<ManagedBuffer>(buffer.Ptr);
            if (b == null || pcm == IntPtr.Zero || bufferSize <= 0) return;

            uint channels = mono ? 1u : 2u;
            // Stride writes int16 interleaved PCM into the buffer.
            using var format = new AVAudioFormat(AVAudioCommonFormat.PCMInt16, sampleRate, channels, interleaved: true);
            uint frameCapacity = (uint)(bufferSize / (2 * channels));
            if (b.Pcm == null || b.Pcm.FrameCapacity < frameCapacity || b.Pcm.Format == null || (int)b.Pcm.Format.SampleRate != sampleRate || b.Pcm.Format.ChannelCount != channels)
            {
                b.Pcm?.Dispose();
                b.Pcm = new AVAudioPcmBuffer(format, frameCapacity);
            }
            // AVAudioPcmBuffer.Int16ChannelData is one pointer per channel; interleaved formats expose
            // a single channel pointer holding all samples interleaved.
            var dst = b.Pcm.Int16ChannelData;
            if (dst != IntPtr.Zero)
            {
                System.Buffer.MemoryCopy((void*)pcm, ((short**)dst)[0], frameCapacity * 2 * channels, bufferSize);
            }
            b.Pcm.FrameLength = frameCapacity;
        }

        // -- Source --------------------------------------------------------------

        public static Source SourceCreate(Listener listener, int sampleRate, int maxNumberOfBuffers, bool mono, bool spatialized, bool streamed, bool hrtf, float hrtfDirectionFactor, HrtfEnvironment environment)
        {
            var l = ResolveHandle<ManagedListener>(listener.Ptr);
            if (l?.Device == null) return default;
            var dev = l.Device;

            var src = new ManagedSource
            {
                Device = dev,
                Listener = l,
                SampleRate = sampleRate,
                Mono = mono,
                Spatialized = spatialized,
                Streamed = streamed,
            };
            uint channels = mono ? 1u : 2u;
            src.Format = new AVAudioFormat(AVAudioCommonFormat.PCMInt16, sampleRate, channels, interleaved: true);
            src.Player = new AVAudioPlayerNode();
            dev.Engine.AttachNode(src.Player);

            // 3D-spatialized mono sources route through the environment node (lazy-created);
            // everything else goes straight to the main mixer.
            if (spatialized && mono)
            {
                EnsureEnv(dev);
                dev.Engine.Connect(src.Player, dev.Env, src.Format);
            }
            else
            {
                dev.Engine.Connect(src.Player, dev.Mixer, src.Format);
            }

            // Streaming sources pre-allocate a pool of empty buffers that SourceGetFreeBuffer
            // hands back to the caller; BufferFill (via the buffer the caller filled) + QueueBuffer
            // recycle them as the player consumes them.
            if (streamed && maxNumberOfBuffers > 0)
            {
                for (int i = 0; i < maxNumberOfBuffers; i++)
                    src.StreamingFree.Enqueue(new ManagedBuffer { CapacityBytes = 0 });
            }

            lock (dev.Sources) dev.Sources.Add(src);
            return new Source { Ptr = AllocHandle(src) };
        }

        public static void SourceDestroy(Source source)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src != null)
            {
                try { src.Player?.Stop(); } catch { }
                try { if (src.Player != null && src.Device != null) src.Device.Engine.DetachNode(src.Player); } catch { }
                if (src.Device != null)
                    lock (src.Device.Sources) src.Device.Sources.Remove(src);
            }
            FreeHandle(source.Ptr);
        }

        public static double SourceGetPosition(Source source)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src?.Player?.LastRenderTime is { } rt && src.SampleRate > 0)
            {
                var playerTime = src.Player.GetPlayerTimeFromNodeTime(rt);
                if (playerTime != null) return (double)playerTime.SampleTime / src.SampleRate;
            }
            return 0;
        }

        public static void SourceSetPan(Source source, float pan)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src?.Player != null) src.Player.Pan = pan;
        }

        public static void SourceSetBuffer(Source source, Buffer buffer)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            var buf = ResolveHandle<ManagedBuffer>(buffer.Ptr);
            if (src == null) return;
            src.StaticBuffer = buf;
        }

        public static void SourceFlushBuffers(Source source)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src?.Player == null) return;
            try { src.Player.Stop(); } catch { }
            // Recycle pending streaming buffers back to the free pool.
            while (src.StreamingPending.Count > 0)
            {
                var b = src.StreamingPending.Dequeue();
                b.Scheduled = false;
                src.StreamingFree.Enqueue(b);
            }
        }

        public static void SourceQueueBuffer(Source source, Buffer buffer, IntPtr pcm, int bufferSize, BufferType streamType)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            var buf = ResolveHandle<ManagedBuffer>(buffer.Ptr);
            if (src?.Player == null || buf == null) return;
            // Caller already wrote PCM into the buffer's AVAudioPcmBuffer via BufferFill. Just schedule.
            buf.StreamType = streamType;
            buf.Scheduled = true;
            src.StreamingPending.Enqueue(buf);
            if (buf.Pcm != null)
            {
                src.Player.ScheduleBuffer(buf.Pcm, () =>
                {
                    // Completion runs on an AV worker thread; bounce back onto a free pool the
                    // game thread polls via SourceGetFreeBuffer.
                    lock (src.StreamingFree)
                    {
                        buf.Scheduled = false;
                        src.StreamingFree.Enqueue(buf);
                    }
                });
            }
        }

        public static Buffer SourceGetFreeBuffer(Source source)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src == null) return default;
            lock (src.StreamingFree)
            {
                if (src.StreamingFree.Count == 0) return default;
                var b = src.StreamingFree.Dequeue();
                return new Buffer { Ptr = AllocHandle(b) };
            }
        }

        public static void SourcePlay(Source source)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src?.Player == null) return;
            // Static (non-streamed) sources schedule the single attached buffer here. The streaming
            // path schedules via SourceQueueBuffer; SourcePlay just resumes the player node.
            if (!src.Streamed && src.StaticBuffer?.Pcm != null && !src.Player.Playing)
            {
                var options = src.Looping ? AVAudioPlayerNodeBufferOptions.Loops : AVAudioPlayerNodeBufferOptions.Interrupts;
                src.Player.ScheduleBuffer(src.StaticBuffer.Pcm, when: null, options, completionHandler: () => { });
            }
            src.Player.Play();
        }

        public static void SourcePause(Source source)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            try { src?.Player?.Pause(); } catch { }
        }

        public static void SourceStop(Source source)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            try { src?.Player?.Stop(); } catch { }
            if (src != null)
            {
                while (src.StreamingPending.Count > 0)
                {
                    var b = src.StreamingPending.Dequeue();
                    b.Scheduled = false;
                    src.StreamingFree.Enqueue(b);
                }
            }
        }

        public static void SourceSetLooping(Source source, bool looped)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src != null) src.Looping = looped;
        }

        public static void SourceSetRange(Source source, double startTime, double stopTime)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src != null)
            {
                src.PlayRangeStart = startTime;
                src.PlayRangeStop = stopTime;
            }
        }

        public static void SourceSetGain(Source source, float gain)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src != null)
            {
                src.Gain = gain;
                if (src.Player != null) src.Player.Volume = gain;
            }
        }

        public static void SourceSetPitch(Source source, float pitch)
        {
            // AVAudioPlayerNode has no rate property; an AVAudioUnitVarispeed node between player
            // and mixer would handle this. Skipped until a real consumer needs it.
        }

        public static void SourcePush3D(Source source, ref Vector3 pos, ref Vector3 forward, ref Vector3 up, ref Vector3 vel, ref Matrix worldTransform)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            if (src?.Player == null) return;
            if (src.Spatialized)
            {
                src.Player.Position = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);
            }
        }

        public static bool SourceIsPlaying(Source source)
        {
            var src = ResolveHandle<ManagedSource>(source.Ptr);
            return src?.Player?.Playing ?? false;
        }
    }
}
#endif
