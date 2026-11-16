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

            public ProbeSoundSource(bool throwOnFill) : base(null, 2, 4096)
            {
                this.throwOnFill = throwOnFill;
            }

            public int FillCount => Volatile.Read(ref fillCount);

            public bool Disposed => Volatile.Read(ref isDisposed);

            public override int MaxNumberOfBuffers => 2;

            public override void SetLooped(bool looped) { }

            public void Attach(SoundInstance instance)
            {
                soundInstance = instance;
                NewSources.Add(this);
            }

            protected override void ExtractAndFillData()
            {
                Interlocked.Increment(ref fillCount);
                if (throwOnFill)
                    throw new InvalidOperationException("Faulty source.");
            }
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

        [Fact]
        public async Task FaultySourceDoesNotStopOtherSources()
        {
            var engine = AudioEngineFactory.NewAudioEngine(new AudioDevice());
            try
            {
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
    }
}
