// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Rendering;

namespace Stride.Animations
{
    /// <summary>
    /// An aggregation of <see cref="AnimationCurve"/> with their channel names.
    /// </summary>
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<AnimationClip>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<AnimationClip>), Profile = "Content")]
    public sealed class AnimationClip
    {
        // If there is an evaluator, animation clip can't be changed anymore.
        internal bool Frozen;

        /// <summary>
        /// Gets or sets the duration of this clip.
        /// </summary>
        /// <value>
        /// The duration of this clip.
        /// </value>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the repeat mode of the <see cref="AnimationClip"/>.
        /// </summary>
        public AnimationRepeatMode RepeatMode { get; set; }

        /// <summary>
        /// Gets the channels of this clip.
        /// </summary>
        /// <value>
        /// The channels of this clip.
        /// </value>
        public Dictionary<string, Channel> Channels = new Dictionary<string, Channel>();

        // TODO: The curve stored inside should be internal/private (it is public now to avoid implementing custom serialization before first release).
        [MemberCollection(NotNullItems = true)]
        public List<AnimationCurve> Curves = new List<AnimationCurve>();

        public AnimationData[] OptimizedAnimationDatas;

        /// <summary>
        /// Set this flag to true when the channel information of the clip have changed and need to be rescan by engine.
        /// </summary>
        [DataMemberIgnore]
        public bool ShouldRescanChannels;

        /// <summary>
        /// Adds a named curve.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="curve">The curve.</param>
        public void AddCurve(string propertyName, AnimationCurve curve, bool isUserCustomProperty = false)
        {
            if (Frozen)
                throw new InvalidOperationException("This AnimationClip is frozen");

            // Add channel
            Channels.Add(propertyName, new Channel
            {
                PropertyName = propertyName,
                CurveIndex = Curves.Count,
                ElementType = curve.ElementType,
                ElementSize = curve.ElementSize,
                IsUserCustomProperty = isUserCustomProperty,
            });
            Curves.Add(curve);
        }

        public AnimationCurve GetCurve(string propertyName)
        {
            Channel channel;
            if (!Channels.TryGetValue(propertyName, out channel))
                return null;

            // Optimized (should we throw exception?)
            if (channel.CurveIndex == -1)
                return null;

            return Curves[channel.CurveIndex];
        }

        /// <summary>
        /// Optimizes data from multiple curves to a single linear data stream.
        /// </summary>
        public void Optimize()
        {
            Freeze();

            // Already optimized?
            if (OptimizedAnimationDatas != null)
                return;

            var optimizedAnimationDatas = new List<AnimationData>();

            // Find Vector3 channels
            foreach (var curveByTypes in Channels
                .Where(x => x.Value.CurveIndex != -1)
                .GroupBy(x => x.Value.ElementType))
            {
                // Create AnimationData
                var firstCurve = Curves[curveByTypes.First().Value.CurveIndex];
                var animationData = firstCurve.CreateOptimizedData(curveByTypes.Select(x => new KeyValuePair<string, AnimationCurve>(x.Key, Curves[x.Value.CurveIndex])));
                optimizedAnimationDatas.Add(animationData);

                // Update channels
                foreach (var curve in curveByTypes)
                {
                    var channel = Channels[curve.Key];

                    Curves[channel.CurveIndex] = null;
                    channel.CurveIndex = -1;

                    Channels[curve.Key] = channel;
                }
            }

            OptimizedAnimationDatas = optimizedAnimationDatas.ToArray();
        }

        internal void Freeze()
        {
            Frozen = true;
        }

        [DataContract]
        public struct Channel
        {
            public string PropertyName;

            public int CurveIndex;
            public Type ElementType;
            public int ElementSize;
            public bool IsUserCustomProperty;
        }
    }
}
