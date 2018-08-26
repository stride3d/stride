// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core.Mathematics;

namespace Xenko.Input
{
    /// <summary>
    /// Event class for the Flick gesture.
    /// </summary>
    public sealed class GestureEventFlick : GestureEventTranslation
    {
        internal void Set(int numberOfFingers, TimeSpan time, GestureShape shape, Vector2 startPos, Vector2 currPos)
        {
            Set(GestureType.Flick, GestureState.Occurred, numberOfFingers, time, time, shape, startPos, currPos, currPos - startPos);
        }
    }
}
