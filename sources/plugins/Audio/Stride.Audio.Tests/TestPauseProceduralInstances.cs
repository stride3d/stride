// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Media;
using Xunit;

namespace Stride.Audio.Tests
{
    /// <summary>
    /// <see cref="AudioEngine.PauseAudio"/> and <see cref="AudioEngine.ResumeAudio"/> on instances created on a <see cref="DynamicSoundSource"/>, which have no <see cref="Sound"/>.
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

        [Fact]
        public void PauseAudioPausesAndResumeAudioResumes()
        {
            var instance = CreatePlayingInstance();

            engine.PauseAudio();
            Assert.Equal(PlayState.Paused, instance.PlayState);

            engine.ResumeAudio();
            Assert.Equal(PlayState.Playing, instance.PlayState);

            instance.Dispose();
        }

        [Fact]
        public void InstancePausedByUserStaysPausedAfterResume()
        {
            var instance = CreatePlayingInstance();
            instance.Pause();

            engine.PauseAudio();
            engine.ResumeAudio();

            Assert.Equal(PlayState.Paused, instance.PlayState);

            instance.Dispose();
        }

        [Fact]
        public void InstanceStoppedDuringEnginePauseStaysStopped()
        {
            var instance = CreatePlayingInstance();

            engine.PauseAudio();
            instance.Stop();
            engine.ResumeAudio();

            Assert.Equal(PlayState.Stopped, instance.PlayState);

            instance.Dispose();
        }

        [Fact]
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

        [Fact]
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
                var source = new ProceduralSource();
                var instance = new SoundInstance(engine, engine.DefaultListener, source, 44100, mono: true);
                source.soundInstance = instance;
                NewSources.Add(source);
                return instance;
            }
        }
    }
}
