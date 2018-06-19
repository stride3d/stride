// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Input;

namespace Xenko.Games.Testing.Requests
{
    [DataContract]
    internal class TapSimulationRequest : TestRequestBase
    {
        public PointerEventType EventType;
        public TimeSpan Delta;
        public Vector2 Coords;
        public Vector2 CoordsDelta;
    }
}
