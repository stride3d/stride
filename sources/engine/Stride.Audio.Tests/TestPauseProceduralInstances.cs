// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Stride.Media;
using Xunit;

namespace Stride.Audio.Tests
{
    /// <summary>
    /// <see cref="AudioEngine.PauseAudio"/> and <see cref="AudioEngine.ResumeAudio"/> on instances created on a <see cref="DynamicSoundSource"/>, which have no <see cref="Sound"/>, and play requests made while the engine is paused.
    /// </summary>
    public class TestPauseProceduralInstances : IDisposable
    {
        private readonly AudioEngine engine;

        public TestPauseProceduralInstances()
        {
            engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
        }

        public void Dispose()
        {
            engine.Dispose();
        }

        private SoundInstance CreatePlayingInstance()
        {
            var instance = ProceduralSource.CreateInstance(engine);
            instance.Play();
            Assert.Equal(PlayState.Playing, instance.PlayState);
            return instance;
        }

        [SkippableFact]
        public void PauseAudioPausesAndResumeAudioResumes()
        {
            var instance = CreatePlayingInstance();

            engine.PauseAudio();
            Assert.Equal(PlayState.Paused, instance.PlayState);

            engine.ResumeAudio();
            Assert.Equal(PlayState.Playing, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void InstancePausedByUserStaysPausedAfterResume()
        {
            var instance = CreatePlayingInstance();
            instance.Pause();

            engine.PauseAudio();
            engine.ResumeAudio();

            Assert.Equal(PlayState.Paused, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void InstanceStoppedDuringEnginePauseStaysStopped()
        {
            var instance = CreatePlayingInstance();

            engine.PauseAudio();
            instance.Stop();
            engine.ResumeAudio();

            Assert.Equal(PlayState.Stopped, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void InstanceDisposedDuringEnginePauseIsSkippedAndUntracked()
        {
            var instance = CreatePlayingInstance();
            Assert.Equal(1, engine.DynamicSourceInstanceCount);

            engine.PauseAudio();
            instance.Dispose();
            Assert.Equal(0, engine.DynamicSourceInstanceCount);

            engine.ResumeAudio();
            Assert.Equal(AudioEngineState.Running, engine.State);
        }

        [SkippableFact]
        public void ProceduralInstanceNotDisposedByUserIsDisposedWithTheEngine()
        {
            var playing = CreatePlayingInstance();
            var idle = ProceduralSource.CreateInstance(engine);
            Assert.Equal(2, engine.DynamicSourceInstanceCount);

            engine.Dispose();

            Assert.True(playing.IsDisposed);
            Assert.True(idle.IsDisposed);
            Assert.Equal(0, engine.DynamicSourceInstanceCount);
        }

        private static void SkipWithoutAudioDevice(AudioEngine engine)
        {
            Skip.If(engine.State == AudioEngineState.Invalidated, "No audio device: the engine cannot create sources.");
        }

        private Sound CreateInMemorySound()
        {
            SkipWithoutAudioDevice(engine);
            const int sampleCount = 44100;
            var sound = new Sound { Channels = 1, SampleRate = 44100, Samples = sampleCount, StreamFromDisk = false };
            sound.Attach(engine);
            sound.PreloadedBuffer = AudioLayer.BufferCreate(sampleCount * sizeof(short));
            var pcm = Marshal.AllocHGlobal(sampleCount * sizeof(short));
            try
            {
                AudioLayer.BufferFill(sound.PreloadedBuffer, pcm, sampleCount * sizeof(short), sound.SampleRate, true);
            }
            finally
            {
                Marshal.FreeHGlobal(pcm);
            }
            return sound;
        }

        [SkippableFact]
        public void ProceduralInstancePlayedDuringPauseStartsOnResume()
        {
            var instance = ProceduralSource.CreateInstance(engine);

            engine.PauseAudio();
            instance.Play();
            Assert.Equal(PlayState.Paused, instance.PlayState);

            engine.ResumeAudio();
            Assert.Equal(PlayState.Playing, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void LoopingInMemoryInstancePlayedDuringPauseStartsOnResume()
        {
            using var sound = CreateInMemorySound();
            var instance = sound.CreateInstance(engine.DefaultListener);
            instance.IsLooping = true;

            engine.PauseAudio();
            instance.Play();
            Assert.Equal(PlayState.Paused, instance.PlayState);

            engine.ResumeAudio();
            Assert.Equal(PlayState.Playing, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void NonLoopingInMemoryInstancePlayedDuringPauseIsDropped()
        {
            using var sound = CreateInMemorySound();
            var instance = sound.CreateInstance(engine.DefaultListener);

            engine.PauseAudio();
            instance.Play();
            Assert.Equal(PlayState.Stopped, instance.PlayState);

            engine.ResumeAudio();
            Assert.Equal(PlayState.Stopped, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void NonLoopingInstancePausedByEngineAndPlayedAgainDuringPauseResumes()
        {
            using var sound = CreateInMemorySound();
            var instance = sound.CreateInstance(engine.DefaultListener);
            instance.Play();
            Assert.Equal(PlayState.Playing, instance.PlayState);

            engine.PauseAudio();
            instance.Pause();
            instance.Play();

            engine.ResumeAudio();
            Assert.Equal(PlayState.Playing, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void DeferredInstanceStoppedDuringPauseStaysStopped()
        {
            var instance = ProceduralSource.CreateInstance(engine);

            engine.PauseAudio();
            instance.Play();
            instance.Stop();
            engine.ResumeAudio();

            Assert.Equal(PlayState.Stopped, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void DeferredInstancePausedDuringPauseStaysPausedAfterResume()
        {
            var instance = ProceduralSource.CreateInstance(engine);

            engine.PauseAudio();
            instance.Play();
            instance.Pause();
            Assert.Equal(PlayState.Paused, instance.PlayState);
            Assert.Equal(0, engine.PausedSoundCount);

            engine.ResumeAudio();
            Assert.Equal(PlayState.Paused, instance.PlayState);

            instance.Play();
            Assert.Equal(PlayState.Playing, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void InstancePausedByEnginePausedAgainByUserStaysPausedAfterResume()
        {
            var instance = CreatePlayingInstance();

            engine.PauseAudio();
            Assert.Equal(PlayState.Paused, instance.PlayState);
            instance.Pause();

            engine.ResumeAudio();
            Assert.Equal(PlayState.Paused, instance.PlayState);

            instance.Dispose();
        }

        [SkippableFact]
        public void DeferredInstanceDisposedDuringPauseDoesNotBreakResume()
        {
            var instance = ProceduralSource.CreateInstance(engine);

            engine.PauseAudio();
            instance.Play();
            instance.Dispose();

            engine.ResumeAudio();
            Assert.Equal(AudioEngineState.Running, engine.State);
        }

        [SkippableFact]
        public void PlayCalledTwiceDuringPauseIsRememberedOnce()
        {
            var instance = ProceduralSource.CreateInstance(engine);

            engine.PauseAudio();
            instance.Play();
            instance.Play();
            Assert.Equal(1, engine.PausedSoundCount);

            engine.ResumeAudio();
            Assert.Equal(PlayState.Playing, instance.PlayState);
            Assert.Equal(0, engine.PausedSoundCount);

            instance.Dispose();
        }

        [SkippableFact]
        public void PlayExclusiveDuringPauseStopsSiblingsAndStartsOnResume()
        {
            using var sound = CreateInMemorySound();
            var playing = sound.CreateInstance(engine.DefaultListener);
            playing.IsLooping = true;
            playing.Play();
            Assert.Equal(PlayState.Playing, playing.PlayState);
            var exclusive = sound.CreateInstance(engine.DefaultListener);
            exclusive.IsLooping = true;

            engine.PauseAudio();
            exclusive.PlayExclusive();
            Assert.Equal(PlayState.Stopped, playing.PlayState);
            Assert.Equal(PlayState.Paused, exclusive.PlayState);

            engine.ResumeAudio();
            Assert.Equal(PlayState.Stopped, playing.PlayState);
            Assert.Equal(PlayState.Playing, exclusive.PlayState);

            playing.Dispose();
            exclusive.Dispose();
        }

        private sealed class ProceduralSource : DynamicSoundSource
        {
            private ProceduralSource() : base(null, 4, 4096)
            {
            }

            public override int MaxNumberOfBuffers => 4;

            protected override bool CanFill => false;

            public override void SetLooped(bool looped)
            {
            }

            protected override void ExtractAndFillData()
            {
            }

            public static SoundInstance CreateInstance(AudioEngine engine)
            {
                SkipWithoutAudioDevice(engine);
                var source = new ProceduralSource();
                var instance = new SoundInstance(engine, engine.DefaultListener, source, 44100, mono: true);
                source.soundInstance = instance;
                NewSources.Add(source);
                return instance;
            }
        }
    }
}
