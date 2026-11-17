// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Threading.Tasks;
using Stride.Media;
using Xunit;

namespace Stride.Audio.Tests
{
    public class DynamicSoundSourceFaultTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private sealed class ProbeSoundSource : DynamicSoundSource
        {
            private readonly bool throwOnFill;
            private int fillCount;
            private int updateCount;
            private int disposeCount;
            private int commandAfterDispose;

            public ProbeSoundSource(bool throwOnFill) : base(null, 2, 4096)
            {
                this.throwOnFill = throwOnFill;
            }

            public int FillCount => Volatile.Read(ref fillCount);

            public int UpdateCount => Volatile.Read(ref updateCount);

            /// <summary>Makes <see cref="DisposeInternal"/> throw after the native source was destroyed, as a failed stream initialisation does.</summary>
            public bool ThrowAfterDispose { get; init; }

            /// <summary>When set, the worker blocks in <see cref="UpdateInternal"/> until <see cref="Release"/> is set.</summary>
            public ManualResetEventSlim Release { get; init; }

            public ManualResetEventSlim Blocked { get; } = new ManualResetEventSlim();

            public int DisposeCount => Volatile.Read(ref disposeCount);

            public bool Disposed => Volatile.Read(ref isDisposed);

            /// <summary>Whether a Play, Pause or Stop command ran after the source was disposed.</summary>
            public bool CommandRanAfterDispose => Volatile.Read(ref commandAfterDispose) != 0;

            public override int MaxNumberOfBuffers => 2;

            public override void SetLooped(bool looped) { }

            public void Attach(SoundInstance instance)
            {
                soundInstance = instance;
                NewSources.Add(this);
            }

            protected override void UpdateInternal()
            {
                Interlocked.Increment(ref updateCount);
                if (Release != null && !Release.IsSet)
                {
                    Blocked.Set();
                    Release.Wait(Timeout);
                }
            }

            protected override void PlayInternal()
            {
                if (RecordCommandAfterDispose()) return;
                base.PlayInternal();
            }

            protected override void PauseInternal()
            {
                if (RecordCommandAfterDispose()) return;
                base.PauseInternal();
            }

            protected override void StopInternal(bool ignoreQueuedBuffer = true)
            {
                if (RecordCommandAfterDispose()) return;
                base.StopInternal(ignoreQueuedBuffer);
            }

            private bool RecordCommandAfterDispose()
            {
                if (!Disposed) return false;
                Volatile.Write(ref commandAfterDispose, 1);
                return true;
            }

            protected override void DisposeInternal()
            {
                Interlocked.Increment(ref disposeCount);
                base.DisposeInternal();
                if (ThrowAfterDispose)
                    throw new InvalidOperationException("Dispose failed after the native source was destroyed.");
            }

            protected override void ExtractAndFillData()
            {
                Interlocked.Increment(ref fillCount);
                if (throwOnFill)
                    throw new InvalidOperationException("Faulty source.");
            }
        }

        private static void SkipWithoutAudioDevice(AudioEngine engine)
        {
            Skip.If(engine.State == AudioEngineState.Invalidated, "No audio device: the engine cannot create sources.");
        }

        private static async Task<bool> WaitUntil(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow + Timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                    return true;
                await Task.Delay(10);
            }
            return condition();
        }

        [SkippableFact]
        public async Task FaultySourceDoesNotStopOtherSources()
        {
            var engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
            try
            {
                SkipWithoutAudioDevice(engine);
                Assert.Equal(AudioEngineState.Running, engine.State);

                var faulty = new ProbeSoundSource(throwOnFill: true);
                var healthy = new ProbeSoundSource(throwOnFill: false);
                var faultyInstance = new SoundInstance(engine, engine.DefaultListener, faulty, 44100, mono: true);
                var healthyInstance = new SoundInstance(engine, engine.DefaultListener, healthy, 44100, mono: true);
                faulty.Attach(faultyInstance);
                healthy.Attach(healthyInstance);

                faulty.Play();
                healthy.Play();

                Assert.True(await WaitUntil(() => faulty.FillCount > 0), "The faulty source was never updated.");
                Assert.True(await Task.WhenAny(faulty.Ended.Task, Task.Delay(Timeout)) == faulty.Ended.Task, "The faulty source did not end.");
                Assert.False(faulty.Ended.Task.Result);
                Assert.False(faulty.IsPausedOrPlaying);
                Assert.Equal(PlayState.Stopped, faultyInstance.PlayState);

                faultyInstance.Dispose();
                Assert.True(await WaitUntil(() => faulty.Disposed), "The faulty source was not disposed by the worker.");

                var countAfterFault = healthy.FillCount;
                Assert.True(await WaitUntil(() => healthy.FillCount > countAfterFault + 2), "The healthy source stopped being updated after another source failed.");
                Assert.Equal(1, faulty.FillCount);

                healthyInstance.Dispose();
            }
            finally
            {
                engine.Dispose();
            }
        }

        [SkippableFact]
        public async Task PlayOnFaultedSourceDoesNotReportPlaying()
        {
            var engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
            try
            {
                SkipWithoutAudioDevice(engine);
                var faulty = new ProbeSoundSource(throwOnFill: true);
                var instance = new SoundInstance(engine, engine.DefaultListener, faulty, 44100, mono: true);
                faulty.Attach(instance);

                faulty.Play();
                Assert.True(await WaitUntil(() => faulty.FillCount > 0 && faulty.Ended.Task.IsCompleted), "The faulty source did not fault.");

                faulty.Play();

                Assert.True(await WaitUntil(() => !faulty.IsPausedOrPlaying), "A faulted source reports playing after Play().");
                Assert.Equal(PlayState.Stopped, instance.PlayState);

                instance.Dispose();
            }
            finally
            {
                engine.Dispose();
            }
        }

        [SkippableFact]
        public async Task SourceThatFailsToDisposeDoesNotCrashNorStopOtherSources()
        {
            var engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
            try
            {
                SkipWithoutAudioDevice(engine);
                var failing = new ProbeSoundSource(throwOnFill: false) { ThrowAfterDispose = true };
                var healthy = new ProbeSoundSource(throwOnFill: false);
                var failingInstance = new SoundInstance(engine, engine.DefaultListener, failing, 44100, mono: true);
                var healthyInstance = new SoundInstance(engine, engine.DefaultListener, healthy, 44100, mono: true);
                failing.Attach(failingInstance);
                healthy.Attach(healthyInstance);
                healthy.Play();

                failingInstance.Dispose();
                Assert.True(await WaitUntil(() => failing.Disposed), "The failing source was not disposed by the worker.");

                var countAfterDispose = healthy.FillCount;
                Assert.True(await WaitUntil(() => healthy.FillCount > countAfterDispose + 2), "The healthy source stopped being updated after another source failed to dispose.");

                healthyInstance.Dispose();
                Assert.True(await WaitUntil(() => healthy.Disposed), "The healthy source was not disposed.");
            }
            finally
            {
                engine.Dispose();
            }
        }

        [SkippableFact]
        public async Task ShutdownDisposesOtherSourcesWhenOneFailsToDispose()
        {
            var engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
            var release = new ManualResetEventSlim();
            try
            {
                SkipWithoutAudioDevice(engine);
                var failing = new ProbeSoundSource(throwOnFill: false) { ThrowAfterDispose = true };
                var healthy = new ProbeSoundSource(throwOnFill: false);
                var blocker = new ProbeSoundSource(throwOnFill: false) { Release = release };
                var failingInstance = new SoundInstance(engine, engine.DefaultListener, failing, 44100, mono: true);
                var healthyInstance = new SoundInstance(engine, engine.DefaultListener, healthy, 44100, mono: true);
                var blockerInstance = new SoundInstance(engine, engine.DefaultListener, blocker, 44100, mono: true);

                // The worker adds sources in the order they were attached: the failing one comes before the healthy one.
                failing.Attach(failingInstance);
                Assert.True(await WaitUntil(() => failing.UpdateCount > 0), "The failing source was never updated.");
                healthy.Attach(healthyInstance);
                Assert.True(await WaitUntil(() => healthy.UpdateCount > 0), "The healthy source was never updated.");

                // The blocker holds the worker so that both Dispose commands are still queued when the shutdown starts.
                blocker.Attach(blockerInstance);
                Assert.True(blocker.Blocked.Wait(Timeout), "The worker never reached the blocker.");
                failing.Dispose();
                healthy.Dispose();

                var shutdown = Task.Run(engine.Dispose);
                await Task.Delay(500);
                release.Set();
                Assert.True(await Task.WhenAny(shutdown, Task.Delay(Timeout)) == shutdown, "The engine did not shut down.");

                Assert.True(failing.Disposed);
                Assert.True(healthy.Disposed, "The shutdown stopped disposing sources after one failed.");
            }
            finally
            {
                release.Set();
                engine.Dispose();
            }
        }

        [SkippableFact]
        public async Task DisposingSourceThenInstanceThenEngineDisposesSourceOnce()
        {
            var engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
            try
            {
                SkipWithoutAudioDevice(engine);
                var source = new ProbeSoundSource(throwOnFill: false);
                var instance = new SoundInstance(engine, engine.DefaultListener, source, 44100, mono: true);
                source.Attach(instance);
                Assert.True(await WaitUntil(() => source.UpdateCount > 0), "The source was never updated.");

                source.Dispose();
                instance.Dispose();
                Assert.True(await WaitUntil(() => source.Disposed), "The source was not disposed by the worker.");

                engine.Dispose();
                Assert.Equal(1, source.DisposeCount);
            }
            finally
            {
                engine.Dispose();
            }
        }

        [SkippableFact]
        public async Task CommandQueuedAfterDisposeDoesNotRun()
        {
            var engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
            var release = new ManualResetEventSlim(true);
            try
            {
                SkipWithoutAudioDevice(engine);
                var source = new ProbeSoundSource(throwOnFill: false) { Release = release };
                var instance = new SoundInstance(engine, engine.DefaultListener, source, 44100, mono: true);
                source.Attach(instance);
                instance.Play();
                Assert.True(await WaitUntil(() => source.IsPausedOrPlaying && source.UpdateCount > 0), "The source never played.");

                // The worker waits in UpdateInternal while Dispose and Stop are queued, so one pass handles both.
                release.Reset();
                Assert.True(source.Blocked.Wait(Timeout), "The worker never reached the source.");
                source.Dispose();
                instance.Dispose();
                release.Set();

                Assert.True(await WaitUntil(() => source.Disposed), "The source was not disposed by the worker.");
                engine.Dispose();
                Assert.Equal(1, source.DisposeCount);
                Assert.False(source.CommandRanAfterDispose, "A command ran on the source after it was disposed.");
            }
            finally
            {
                release.Set();
                engine.Dispose();
            }
        }

        [SkippableFact]
        public async Task DisposingSourceTwiceDisposesItOnce()
        {
            var engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
            try
            {
                SkipWithoutAudioDevice(engine);
                var source = new ProbeSoundSource(throwOnFill: false);
                var instance = new SoundInstance(engine, engine.DefaultListener, source, 44100, mono: true);
                source.Attach(instance);
                Assert.True(await WaitUntil(() => source.UpdateCount > 0), "The source was never updated.");

                source.Dispose();
                source.Dispose();
                Assert.True(await WaitUntil(() => source.Disposed), "The source was not disposed by the worker.");

                engine.Dispose();
                Assert.Equal(1, source.DisposeCount);
            }
            finally
            {
                engine.Dispose();
            }
        }
    }
}
