// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Engine;

namespace SpaceEscape.Background
{
    public class BackgroundInfo : ScriptComponent
    {
        public BackgroundInfo()
        {
            Holes = new List<Hole>();
        }

        public int MaxNbObstacles { get; set; }
        public List<Hole> Holes { get; private set; }
    }
}
