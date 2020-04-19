// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Collections;
using Stride.Core.Mathematics;

namespace Stride.Animations
{
    /// <summary>
    /// Evaluates <see cref="AnimationClip"/> to a <see cref="AnimationClipResult"/> at a given time.
    /// </summary>
    public sealed class AnimationClipEvaluator
    {
        private AnimationClip clip;
        internal List<AnimationBlender.Channel> BlenderChannels;

        private FastListStruct<AnimationCurveEvaluatorGroup> curveEvaluatorGroups = new FastListStruct<AnimationCurveEvaluatorGroup>(4);

        // Temporarily exposed for MeshAnimationUpdater
        internal FastListStruct<EvaluatorChannel> Channels = new FastListStruct<EvaluatorChannel>(4);

        public AnimationClip Clip
        {
            get { return clip; }
        }

        internal AnimationClipEvaluator()
        {
        }

        internal void Initialize(AnimationClip clip, List<AnimationBlender.Channel> channels)
        {
            this.BlenderChannels = channels;
            this.clip = clip;
            clip.Freeze();

            // If there are optimized curve data, instantiate (first time) and initialize appropriate evaluators
            if (clip.OptimizedAnimationDatas != null)
            {
                foreach (var optimizedData in clip.OptimizedAnimationDatas)
                {
                    var optimizedEvaluatorGroup = curveEvaluatorGroups.OfType<AnimationCurveEvaluatorOptimizedGroup>().FirstOrDefault(x => x.ElementType == optimizedData.ElementType);
                    if (optimizedEvaluatorGroup == null)
                    {
                        optimizedEvaluatorGroup = optimizedData.CreateEvaluator();
                        curveEvaluatorGroups.Add(optimizedEvaluatorGroup);
                    }
                    optimizedEvaluatorGroup.Initialize(optimizedData);
                }
            }

            // Add already existing channels
            for (int index = 0; index < channels.Count; index++)
            {
                var channel = channels[index];
                AddChannel(ref channel);
            }
        }

        internal void Cleanup()
        {
            foreach (var curveEvaluatorGroup in curveEvaluatorGroups)
            {
                curveEvaluatorGroup.Cleanup();
            }

            Channels.Clear();
            BlenderChannels = null;
            clip = null;
        }

        public unsafe void Compute(CompressedTimeSpan newTime, AnimationClipResult result)
        {
            fixed (byte* structures = result.Data)
            {
                // Update factors
                for (int index = 0; index < Channels.Count; index++)
                {
                    // For now, objects are not supported, so treat everything as a blittable struct.
                    var channel = Channels.Items[index];

                    if (channel.BlendType == AnimationBlender.BlendType.Object)
                    {
                        // Non-blittable (note: 0.0f representation is 0 and that's what UpdateEngine checks against)
                        result.Objects[channel.Offset].Condition = *(int*)&channel.Factor;
                    }
                    else
                    {
                        // Blittable
                        var structureStart = (float*)(structures + channel.Offset);

                        // Write a float specifying channel factor (1 if exists, 0 if doesn't exist)
                        *structureStart = channel.Factor;
                    }
                }

                // Update values
                foreach (var curveEvaluatorGroup in curveEvaluatorGroups)
                {
                    curveEvaluatorGroup.Evaluate(newTime, (IntPtr)structures, result.Objects);
                }
            }
        }

        public unsafe void AddCurveValues(CompressedTimeSpan newTime, AnimationClipResult result)
        {
            fixed (byte* structures = result.Data)
            {
                for (int index = 0; index < Channels.Count; index++)
                {
                    var channel = Channels.Items[index];

                    // For now, objects are not supported, so treat everything as a blittable struct.
                    channel.Curve?.AddValue(newTime, (IntPtr)(structures + channel.Offset + sizeof(float)));
                }
            }
        }

        internal void AddChannel(ref AnimationBlender.Channel channel)
        {
            AnimationClip.Channel clipChannel;
            AnimationCurve curve = null;

            // Try to find curve and create evaluator
            // (if curve doesn't exist, Evaluator will be null).
            bool itemFound = clip.Channels.TryGetValue(channel.PropertyName, out clipChannel);

            if (itemFound)
            {
                var offset = channel.Offset;

                // Object uses array indices, but blittable types are placed after a float that specify if it should be copied
                if (channel.BlendType != AnimationBlender.BlendType.Object)
                    offset += sizeof(float);

                if (clipChannel.CurveIndex != -1)
                {
                    curve = clip.Curves[clipChannel.CurveIndex];

                    // TODO: Optimize this search?
                    var curveEvaluatorGroup = curveEvaluatorGroups.OfType<AnimationCurveEvaluatorDirectGroup>().FirstOrDefault(x => x.ElementType == clipChannel.ElementType);
                    if (curveEvaluatorGroup == null)
                    {
                        // First time, let's create it
                        curveEvaluatorGroup = curve.CreateEvaluator();
                        curveEvaluatorGroups.Add(curveEvaluatorGroup);
                    }

                    curveEvaluatorGroup.AddChannel(curve, offset);
                }
                else
                {
                    // TODO: Optimize this search?
                    var curveEvaluatorGroup = curveEvaluatorGroups.OfType<AnimationCurveEvaluatorOptimizedGroup>().First(x => x.ElementType == clipChannel.ElementType);

                    curveEvaluatorGroup.SetChannelOffset(channel.PropertyName, offset);
                }
            }

            Channels.Add(new EvaluatorChannel { Offset = channel.Offset, BlendType = channel.BlendType, Curve = curve, Factor = itemFound ? 1.0f : 0.0f });
        }

        internal struct EvaluatorChannel
        {
            public int Offset;
            public AnimationBlender.BlendType BlendType;
            public AnimationCurve Curve;
            public float Factor;
        }
    }
}
