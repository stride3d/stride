// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Audio.Tests
{
    class SoundGenerator
    {
        private float t;

        public byte[] Generate(int soundFreq, float[] signalFreq, int nbBytesPerSample, int length)
        {
            if (nbBytesPerSample < 0 || nbBytesPerSample > 2)
                throw new ArgumentException("nbBytesPerSample must be 1 (8bits) or 2 (16bits)");

            if (signalFreq.Length < 0 || signalFreq.Length > 2)
                throw new ArgumentException("Only mono and stereo data are supported");

            if (length % (signalFreq.Length * nbBytesPerSample) != 0)
                throw new ArgumentException("buffer size do not respect alignment constraints.");

            var buffer = new byte[length];

            var timeStep = 1f / soundFreq;

            for (int i = 0; i < buffer.Length; )
            {
                for (int j = 0; j < signalFreq.Length; j++)
                {
                    var s = Math.Sin(signalFreq[j] * t);

                    if (nbBytesPerSample == 1)
                    {
                        buffer[i] = (byte)(127.0 * s + 127.0);
                    }
                    else if (nbBytesPerSample == 2)
                    {
                        var valShort = (short)(short.MaxValue * s);
                        buffer[i] = (byte)(valShort & 0xFF);
                        buffer[i + 1] = (byte)(valShort >> 8);
                    }

                    i += nbBytesPerSample;
                }

                t += timeStep;
            }

            return buffer;
        }
    }
}
