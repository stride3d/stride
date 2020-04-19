// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Stride.Graphics;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Allow readback a Texture from GPU to CPU with a frame delay count to avoid blocking read.
    /// </summary>
    /// <typeparam name="T">Pixel struct that should match the input texture format</typeparam>
    /// <remarks>The input texture should be small enough to avoid CPU/GPU readback stalling</remarks>
    public class ImageReadback<T> : ImageEffect where T : struct
    {
        private readonly List<Texture> stagingTargets;

        private readonly List<bool> stagingUsed;
        private int currentStagingIndex;
        private T[] result;

        private Texture inputTexture;

        private int frameDelayCount;
        private int previousStageCount;

        private readonly Stopwatch clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageReadback{T}"/> class.
        /// </summary>
        public ImageReadback()
        {
            stagingUsed = new List<bool>();
            stagingTargets = new List<Texture>();
            FrameDelayCount = 16;
            clock = new Stopwatch();
        }

        /// <summary>
        /// Gets or sets the number of frame to store before reading back. Default is <c>16</c>.
        /// </summary>
        /// <value>The frame delay count.</value>
        public int FrameDelayCount
        {
            get
            {
                return frameDelayCount;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Expecting a value > 0");
                }

                frameDelayCount = value;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether a result is available from <see cref="Result"/>.
        /// </summary>
        /// <value>A result available.</value>
        public bool IsResultAvailable { get; private set; }

        /// <summary>
        /// Gets a boolean indicating whether the readback is slow and may be stalling, indicating a <see cref="FrameDelayCount"/> to low or
        /// an input texture too large for an efficient non-blocking readback.
        /// </summary>
        /// <value>The readback is slow and stalling.</value>
        public bool IsSlow { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether [force get latest blocking].
        /// </summary>
        /// <value><c>true</c> if [force get latest blocking]; otherwise, <c>false</c>.</value>
        public bool ForceGetLatestBlocking { get; set; }

        /// <summary>
        /// Gets the elapsed time to query the result.
        /// </summary>
        /// <value>The elapsed time.</value>
        public TimeSpan ElapsedTime
        {
            get
            {
                return clock.Elapsed;
            }
        }

        /// <summary>
        /// Gets the result pixels, only valid if <see cref="IsResultAvailable"/>
        /// </summary>
        /// <value>The result.</value>
        public T[] Result
        {
            get
            {
                return result;
            }
        }

        public override void Reset()
        {
            // Make sure that StagingUsed is reseted
            for (int i = 0; i < stagingUsed.Count; i++)
            {
                stagingUsed[i] = false;
            }

            base.Reset();
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var input = GetSafeInput(0);

            // Start the clock
            clock.Restart();

            // Make sure that we have all staging prepared
            EnsureStaging(input);

            // Copy to staging resource
            context.CommandList.Copy(input, stagingTargets[currentStagingIndex]);
            stagingUsed[currentStagingIndex] = true;

            // Read-back to CPU using a ring of staging buffers
            IsResultAvailable = false;
            IsSlow = false;

            if (ForceGetLatestBlocking)
            {
                stagingTargets[currentStagingIndex].GetData(context.CommandList, result);
                IsResultAvailable = true;
                IsSlow = true;
            }
            else
            {
                for (int i = stagingTargets.Count - 1; !IsResultAvailable && i >= 0; i--)
                {
                    var oldStagingIndex = (currentStagingIndex + i) % stagingTargets.Count;
                    var stagingTarget = stagingTargets[oldStagingIndex];

                    // Only try to get data from staging if it has received a copy of the input texture
                    if (stagingUsed[oldStagingIndex])
                    {
                        // If oldest staging target?
                        if (i == 0)
                        {
                            // Get data blocking (otherwise we would loop without getting any readback if StagingCount is not enough high)
                            stagingTarget.GetData(context.CommandList, result);
                            IsSlow = true;
                            IsResultAvailable = true;
                        }
                        else if (stagingTarget.GetData(context.CommandList, result, 0, 0, true)) // Get data non-blocking
                        {
                            IsResultAvailable = true;
                        }
                    }
                }

                // Move to next staging target
                currentStagingIndex = (currentStagingIndex + 1) % stagingTargets.Count;
            }

            // Stop the clock.
            clock.Stop();
        }

        private void EnsureStaging(Texture input)
        {
            // Create all staging texture if input is changing
            if (inputTexture != input || previousStageCount != frameDelayCount)
            {
                DisposeStaging();

                if (input != null)
                {
                    // Allocate result data
                    result = new T[input.CalculatePixelDataCount<T>()];

                    for (int i = 0; i < FrameDelayCount; i++)
                    {
                        stagingTargets.Add(input.ToStaging());
                        stagingUsed.Add(false);
                    }
                }

                previousStageCount = frameDelayCount;
                inputTexture = input;
            }
        }

        private void DisposeStaging()
        {
            foreach (var stagingTarget in stagingTargets)
            {
                stagingTarget.Dispose();
            }
            stagingUsed.Clear();
            stagingTargets.Clear();
        }

        protected override void Destroy()
        {
            DisposeStaging();
            base.Destroy();
        }
    }
}
