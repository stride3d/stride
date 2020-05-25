// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using System;
namespace Stride.Core.Threading
{
    public partial class ThreadPool
    {
        /// <summary>
        /// Hill climbing algorithm used for determining the number of threads needed for the thread pool.
        /// </summary>
        private class HillClimbing
        {
            private const int DefaultSampleIntervalMsLow = 10;
            private const int DefaultSampleIntervalMsHigh = 200;

            private readonly ThreadPool pool;

            private readonly int wavePeriod;
            private readonly int samplesToMeasure;
            private readonly double targetThroughputRatio;
            private readonly double targetSignalToNoiseRatio;
            private readonly double maxChangePerSecond;
            private readonly double maxChangePerSample;
            private readonly int maxThreadWaveMagnitude;
            private readonly int sampleIntervalMsLow;
            private readonly double threadMagnitudeMultiplier;
            private readonly int sampleIntervalMsHigh;
            private readonly double throughputErrorSmoothingFactor;
            private readonly double gainExponent;
            private readonly double maxSampleError;

            private double currentControlSetting;
            private long totalSamples;
            private int lastThreadCount;
            private double averageThroughputNoise;
            private int accumulatedCompletionCount;
            private double accumulatedSampleDurationSeconds;
            private readonly double[] samples;
            private readonly double[] threadCounts;
            private int currentSampleMs;

            private readonly Random randomIntervalGenerator = new Random();
            
            public HillClimbing(ThreadPool pool) : this(
                wavePeriodParam: 4,
                maxWaveMagnitudeParam: 20,
                waveMagnitudeMultiplierParam: 100 / 100.0,
                waveHistorySizeParam: 8,
                targetThroughputRatioParam: 15.0 / 100.0,
                targetSignalToNoiseRatioParam: 300.0 / 100.0,
                maxChangePerSecondParam: 4,
                maxChangePerSampleParam: 20,
                sampleIntervalMsLowParam: DefaultSampleIntervalMsLow,
                sampleIntervalMsHighParam: DefaultSampleIntervalMsHigh,
                errorSmoothingFactorParam: 1.0 / 100.0,
                gainExponentParam: 200.0 / 100.0,
                maxSampleErrorParam: 15.0 / 100.0,
                poolParam: pool)
            {
                
            }
            
            public HillClimbing(int wavePeriodParam, int maxWaveMagnitudeParam, double waveMagnitudeMultiplierParam, int waveHistorySizeParam, double targetThroughputRatioParam,
                double targetSignalToNoiseRatioParam, double maxChangePerSecondParam, double maxChangePerSampleParam, int sampleIntervalMsLowParam, int sampleIntervalMsHighParam,
                double errorSmoothingFactorParam, double gainExponentParam, double maxSampleErrorParam, ThreadPool poolParam)
            {
                wavePeriod = wavePeriodParam;
                maxThreadWaveMagnitude = maxWaveMagnitudeParam;
                threadMagnitudeMultiplier = waveMagnitudeMultiplierParam;
                samplesToMeasure = wavePeriodParam * waveHistorySizeParam;
                targetThroughputRatio = targetThroughputRatioParam;
                targetSignalToNoiseRatio = targetSignalToNoiseRatioParam;
                maxChangePerSecond = maxChangePerSecondParam;
                maxChangePerSample = maxChangePerSampleParam;
                if (sampleIntervalMsLowParam <= sampleIntervalMsHighParam)
                {
                    sampleIntervalMsLow = sampleIntervalMsLowParam;
                    sampleIntervalMsHigh = sampleIntervalMsHighParam;
                }
                else
                {
                    sampleIntervalMsLow = DefaultSampleIntervalMsLow;
                    sampleIntervalMsHigh = DefaultSampleIntervalMsHigh;
                }
                throughputErrorSmoothingFactor = errorSmoothingFactorParam;
                gainExponent = gainExponentParam;
                maxSampleError = maxSampleErrorParam;

                samples = new double[samplesToMeasure];
                threadCounts = new double[samplesToMeasure];

                currentSampleMs = randomIntervalGenerator.Next(sampleIntervalMsLow, sampleIntervalMsHigh + 1);
                pool = poolParam;
            }

            public (int newThreadCount, int newSampleMs) Update(int currentThreadCount, double sampleDurationSeconds, int numCompletions)
            {

                //
                // If someone changed the thread count without telling us, update our records accordingly.
                //
                if (currentThreadCount != lastThreadCount)
                    ForceChange(currentThreadCount);

                //
                // Update the cumulative stats for this thread count
                //

                //
                // Add in any data we've already collected about this sample
                //
                sampleDurationSeconds += accumulatedSampleDurationSeconds;
                numCompletions += accumulatedCompletionCount;

                //
                // We need to make sure we're collecting reasonably accurate data.  Since we're just counting the end
                // of each work item, we are goinng to be missing some data about what really happened during the
                // sample interval.  The count produced by each thread includes an initial work item that may have
                // started well before the start of the interval, and each thread may have been running some new
                // work item for some time before the end of the interval, which did not yet get counted.  So
                // our count is going to be off by +/- threadCount workitems.
                //
                // The exception is that the thread that reported to us last time definitely wasn't running any work
                // at that time, and the thread that's reporting now definitely isn't running a work item now.  So
                // we really only need to consider threadCount-1 threads.
                //
                // Thus the percent error in our count is +/- (threadCount-1)/numCompletions.
                //
                // We cannot rely on the frequency-domain analysis we'll be doing later to filter out this error, because
                // of the way it accumulates over time.  If this sample is off by, say, 33% in the negative direction,
                // then the next one likely will be too.  The one after that will include the sum of the completions
                // we missed in the previous samples, and so will be 33% positive.  So every three samples we'll have
                // two "low" samples and one "high" sample.  This will appear as periodic variation right in the frequency
                // range we're targeting, which will not be filtered by the frequency-domain translation.
                //
                if (totalSamples > 0 && ((currentThreadCount - 1.0) / numCompletions) >= maxSampleError)
                {
                    // not accurate enough yet.  Let's accumulate the data so far, and tell the ThreadPool
                    // to collect a little more.
                    accumulatedSampleDurationSeconds = sampleDurationSeconds;
                    accumulatedCompletionCount = numCompletions;
                    return (currentThreadCount, 10);
                }

                //
                // We've got enouugh data for our sample; reset our accumulators for next time.
                //
                accumulatedSampleDurationSeconds = 0;
                accumulatedCompletionCount = 0;

                //
                // Add the current thread count and throughput sample to our history
                //
                double throughput = numCompletions / sampleDurationSeconds;

                int sampleIndex = (int)(totalSamples % samplesToMeasure);
                samples[sampleIndex] = throughput;
                threadCounts[sampleIndex] = currentThreadCount;
                totalSamples++;

                //
                // Set up defaults for our metrics
                //
                Complex ratio = default;
                double confidence = 0;

                //
                // How many samples will we use?  It must be at least the three wave periods we're looking for, and it must also be a whole
                // multiple of the primary wave's period; otherwise the frequency we're looking for will fall between two  frequency bands
                // in the Fourier analysis, and we won't be able to measure it accurately.
                //
                int sampleCount = ((int)Math.Min(totalSamples - 1, samplesToMeasure)) / wavePeriod * wavePeriod;

                if (sampleCount > wavePeriod)
                {
                    //
                    // Average the throughput and thread count samples, so we can scale the wave magnitudes later.
                    //
                    double sampleSum = 0;
                    double threadSum = 0;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        sampleSum += samples[(totalSamples - sampleCount + i) % samplesToMeasure];
                        threadSum += threadCounts[(totalSamples - sampleCount + i) % samplesToMeasure];
                    }
                    double averageThroughput = sampleSum / sampleCount;
                    double averageThreadCount = threadSum / sampleCount;

                    if (averageThroughput > 0 && averageThreadCount > 0)
                    {
                        //
                        // Calculate the periods of the adjacent frequency bands we'll be using to measure noise levels.
                        // We want the two adjacent Fourier frequency bands.
                        //
                        double adjacentPeriod1 = sampleCount / (((double)sampleCount / wavePeriod) + 1);
                        double adjacentPeriod2 = sampleCount / (((double)sampleCount / wavePeriod) - 1);

                        //
                        // Get the the three different frequency components of the throughput (scaled by average
                        // throughput).  Our "error" estimate (the amount of noise that might be present in the
                        // frequency band we're really interested in) is the average of the adjacent bands.
                        //
                        Complex throughputWaveComponent = GetWaveComponent(samples, sampleCount, wavePeriod) / averageThroughput;
                        double throughputErrorEstimate = (GetWaveComponent(samples, sampleCount, adjacentPeriod1) / averageThroughput).Abs();
                        if (adjacentPeriod2 <= sampleCount)
                        {
                            throughputErrorEstimate = Math.Max(throughputErrorEstimate, (GetWaveComponent(samples, sampleCount, adjacentPeriod2) / averageThroughput).Abs());
                        }

                        //
                        // Do the same for the thread counts, so we have something to compare to.  We don't measure thread count
                        // noise, because there is none; these are exact measurements.
                        //
                        Complex threadWaveComponent = GetWaveComponent(threadCounts, sampleCount, wavePeriod) / averageThreadCount;

                        //
                        // Update our moving average of the throughput noise.  We'll use this later as feedback to
                        // determine the new size of the thread wave.
                        //
                        if (averageThroughputNoise == 0)
                            averageThroughputNoise = throughputErrorEstimate;
                        else
                            averageThroughputNoise = (throughputErrorSmoothingFactor * throughputErrorEstimate) + ((1.0 - throughputErrorSmoothingFactor) * averageThroughputNoise);

                        if (threadWaveComponent.Abs() > 0)
                        {
                            //
                            // Adjust the throughput wave so it's centered around the target wave, and then calculate the adjusted throughput/thread ratio.
                            //
                            ratio = (throughputWaveComponent - (targetThroughputRatio * threadWaveComponent)) / threadWaveComponent;
                        }
                        else
                        {
                            ratio = new Complex(0, 0);
                        }

                        //
                        // Calculate how confident we are in the ratio.  More noise == less confident.  This has
                        // the effect of slowing down movements that might be affected by random noise.
                        //
                        double noiseForConfidence = Math.Max(averageThroughputNoise, throughputErrorEstimate);
                        if (noiseForConfidence > 0)
                            confidence = (threadWaveComponent.Abs() / noiseForConfidence) / targetSignalToNoiseRatio;
                        else
                            confidence = 1.0; //there is no noise!

                    }
                }

                //
                // We use just the real part of the complex ratio we just calculated.  If the throughput signal
                // is exactly in phase with the thread signal, this will be the same as taking the magnitude of
                // the complex move and moving that far up.  If they're 180 degrees out of phase, we'll move
                // backward (because this indicates that our changes are having the opposite of the intended effect).
                // If they're 90 degrees out of phase, we won't move at all, because we can't tell whether we're
                // having a negative or positive effect on throughput.
                //
                double move = Math.Min(1.0, Math.Max(-1.0, ratio.Real));

                //
                // Apply our confidence multiplier.
                //
                move *= Math.Min(1.0, Math.Max(0.0, confidence));

                //
                // Now apply non-linear gain, such that values around zero are attenuated, while higher values
                // are enhanced.  This allows us to move quickly if we're far away from the target, but more slowly
                // if we're getting close, giving us rapid ramp-up without wild oscillations around the target.
                //
                double gain = maxChangePerSecond * sampleDurationSeconds;
                move = Math.Pow(Math.Abs(move), gainExponent) * (move >= 0.0 ? 1 : -1) * gain;
                move = Math.Min(move, maxChangePerSample);

                //
                // If the result was positive, and CPU is > 95%, refuse the move.
                //
                if (move > 0.0 && pool.cpuUtilization > CpuUtilizationHigh)
                    move = 0.0;

                //
                // Apply the move to our control setting
                //
                currentControlSetting += move;

                //
                // Calculate the new thread wave magnitude, which is based on the moving average we've been keeping of
                // the throughput error.  This average starts at zero, so we'll start with a nice safe little wave at first.
                //
                int newThreadWaveMagnitude = (int)(0.5 + (currentControlSetting * averageThroughputNoise * targetSignalToNoiseRatio * threadMagnitudeMultiplier * 2.0));
                newThreadWaveMagnitude = Math.Min(newThreadWaveMagnitude, maxThreadWaveMagnitude);
                newThreadWaveMagnitude = Math.Max(newThreadWaveMagnitude, 1);

                //
                // Make sure our control setting is within the ThreadPool's limits
                //
                int maxThreads = pool.maxThreads;
                int minThreads = pool.minThreads;

                currentControlSetting = Math.Min(maxThreads - newThreadWaveMagnitude, currentControlSetting);
                currentControlSetting = Math.Max(minThreads, currentControlSetting);

                //
                // Calculate the new thread count (control setting + square wave)
                //
                int newThreadCount = (int)(currentControlSetting + newThreadWaveMagnitude * ((totalSamples / (wavePeriod / 2)) % 2));

                //
                // Make sure the new thread count doesn't exceed the ThreadPool's limits
                //
                newThreadCount = Math.Min(maxThreads, newThreadCount);
                newThreadCount = Math.Max(minThreads, newThreadCount);


                //
                // If all of this caused an actual change in thread count, log that as well.
                //
                if (newThreadCount != currentThreadCount)
                    ChangeThreadCount(newThreadCount);

                //
                // Return the new thread count and sample interval.  This is randomized to prevent correlations with other periodic
                // changes in throughput.  Among other things, this prevents us from getting confused by Hill Climbing instances
                // running in other processes.
                //
                // If we're at minThreads, and we seem to be hurting performance by going higher, we can't go any lower to fix this.  So
                // we'll simply stay at minThreads much longer, and only occasionally try a higher value.
                //
                int newSampleInterval;
                if (ratio.Real < 0.0 && newThreadCount == minThreads)
                    newSampleInterval = (int)(0.5 + currentSampleMs * (10.0 * Math.Max(-ratio.Real, 1.0)));
                else
                    newSampleInterval = currentSampleMs;

                return (newThreadCount, newSampleInterval);
            }

            public void ForceChange(int newThreadCount)
            {
                if (lastThreadCount != newThreadCount)
                {
                    currentControlSetting += newThreadCount - lastThreadCount;
                    ChangeThreadCount(newThreadCount);
                }
            }

            private void ChangeThreadCount(int newThreadCount)
            {
                lastThreadCount = newThreadCount;
                currentSampleMs = randomIntervalGenerator.Next(sampleIntervalMsLow, sampleIntervalMsHigh + 1);
            }

            private Complex GetWaveComponent(double[] samples, int numSamples, double period)
            {
                Debug.Assert(numSamples >= period); // can't measure a wave that doesn't fit
                Debug.Assert(period >= 2); // can't measure above the Nyquist frequency
                Debug.Assert(numSamples <= samples.Length); // can't measure more samples than we have

                //
                // Calculate the sinusoid with the given period.
                // We're using the Goertzel algorithm for this.  See http://en.wikipedia.org/wiki/Goertzel_algorithm.
                //

                double w = 2 * Math.PI / period;
                double cos = Math.Cos(w);
                double coeff = 2 * cos;
                double q1 = 0, q2 = 0;
                for (int i = 0; i < numSamples; ++i)
                {
                    double q0 = coeff * q1 - q2 + samples[(totalSamples - numSamples + i) % samplesToMeasure];
                    q2 = q1;
                    q1 = q0;
                }
                return new Complex(q1 - q2 * cos, q2 * Math.Sin(w)) / numSamples;
            }
            
            private struct Complex
            {
                public Complex(double real, double imaginary)
                {
                    Real = real;
                    Imaginary = imaginary;
                }

                public double Imaginary;
                public double Real;

                public static Complex operator *(double scalar, Complex complex) => new Complex(scalar * complex.Real, scalar * complex.Imaginary);

                public static Complex operator *(Complex complex, double scalar) => scalar * complex;

                public static Complex operator /(Complex complex, double scalar) => new Complex(complex.Real / scalar, complex.Imaginary / scalar);

                public static Complex operator -(Complex lhs, Complex rhs) => new Complex(lhs.Real - rhs.Real, lhs.Imaginary - rhs.Imaginary);

                public static Complex operator /(Complex lhs, Complex rhs)
                {
                    double denom = rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary;
                    return new Complex((lhs.Real * rhs.Real + lhs.Imaginary * rhs.Imaginary) / denom, (-lhs.Real * rhs.Imaginary + lhs.Imaginary * rhs.Real) / denom);
                }

                public double Abs() => Math.Sqrt(Real * Real + Imaginary * Imaginary);
            }
        }
    }
}
