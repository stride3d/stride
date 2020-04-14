// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Event class for the Drag gesture.
    /// </summary>
    public sealed class GestureEventDrag : GestureEventTranslation
    {
        internal void Set(GestureState state, int numberOfFingers, TimeSpan deltaTime, TimeSpan totalTime,
                                    GestureShape shape, Vector2 startPos, Vector2 currPos, Vector2 deltaTrans)
        {
            Set(GestureType.Drag, state, numberOfFingers, deltaTime, totalTime, shape, startPos, currPos, deltaTrans);
        }
    }
}
