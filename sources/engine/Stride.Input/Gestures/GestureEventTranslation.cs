// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Event class for the Drag gesture.
    /// </summary>
    public class GestureEventTranslation : GestureEvent
    {
        /// <summary>
        /// The Shape of the drag.
        /// </summary>
        public GestureShape Shape { get; internal set; }

        /// <summary>
        /// The position where the drag event started.
        /// </summary>
        public Vector2 StartPosition { get; internal set; }

        /// <summary>
        /// The current position of the drag event.
        /// </summary>
        public Vector2 CurrentPosition { get; internal set; }

        /// <summary>
        /// The translation performed since the last event of the gesture.
        /// </summary>
        public Vector2 DeltaTranslation { get; internal set; }

        /// <summary>
        /// The translation performed since the beginning of the gesture.
        /// </summary>
        public Vector2 TotalTranslation { get; internal set; }

        /// <summary>
        /// The average translation speed (in pixels per seconds) of the drag event.
        /// </summary>
        public Vector2 AverageSpeed { get; internal set; }

        internal void Set(GestureType type, GestureState state, int numberOfFingers, TimeSpan deltaTime, TimeSpan totalTime, 
                                          GestureShape shape, Vector2 startPos, Vector2 currPos, Vector2 deltaTrans)
        {
            Type = type;
            State = state;
            NumberOfFinger = numberOfFingers;
            DeltaTime = deltaTime;
            TotalTime = totalTime;
            Shape = shape;
            StartPosition = startPos;
            CurrentPosition = currPos;
            DeltaTranslation = deltaTrans;
            TotalTranslation = currPos - startPos;
            AverageSpeed = TotalTranslation / (float)(TotalTime.TotalSeconds + 0.0001f); // avoid division by zero
        }
    }
}
