// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Audio.Tests.Engine;
using Stride.Engine;
using Stride.Media;
using Xunit;

namespace Stride.Audio.Tests;

public class TestCustomBufferSoundSource
{
    [Fact]
    public void TestCustomAudioSource()
    {
        SoundInstance testInstance;
        TestUtilities.ExecuteScriptInUpdateLoop(game =>
            {
                // Create custom audio source
                var mySource = new MyCustomAudioSource();

                // Create the sound, spacialized sounds must be mono
                var sound = new StreamedBufferSound(game.Audio.AudioEngine, mySource, spatialized: false);

                // Create a sound instance
                testInstance = sound.CreateInstance();
                testInstance.SetRange(new PlayRange(TimeSpan.Zero, TimeSpan.FromMilliseconds(500)));
                testInstance.Play(); // Should hear a 440hz tone
            },
            TestUtilities.ExitGameAfterSleep(2000)
        );
    }

    class MyCustomAudioSource : CustomAudioSourceBase
    {
        // Callback from the audio engine
        public override bool ComputeAudioData(AudioData bufferToFill, out bool endOfStream)
        {
            // Create audio data
            GenerateSineWave(bufferToFill.Data);

            bufferToFill.CountDataBytes += BlockSizeInBytes;

            endOfStream = false;
            return true; // success
        }

        public float Frequency = 440f;

        float phase = 0;
        float left, right;
        private void GenerateSineWave(WaveBuffer buffer)
        {

            var channels = Channels;
            var samples = buffer.ShortBufferCount;

            var increment = Frequency / SampleRate;
            for (int i = 0; i < samples; i += channels)
            {
                phase += increment;

                if (phase > 1.0f)
                    phase -= 1.0f;

                left = right = MathF.Sin(phase * MathF.PI * 2);

                buffer.ShortBuffer[i] = (short)(left * short.MaxValue);
                buffer.ShortBuffer[i + 1] = (short)(right * short.MaxValue);
            }
        }
    }
}
