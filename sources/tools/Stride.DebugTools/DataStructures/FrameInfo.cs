// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Framework.MicroThreading;

namespace Stride.DebugTools.DataStructures
{
    public class FrameInfo
    {
        public uint FrameNumber { get; set; }
        public double BeginTime { get; set; }
        public double EndTime { get; set; }
        public List<ThreadInfo> ThreadItems { get; private set; }

        public FrameInfo()
        {
            ThreadItems = new List<ThreadInfo>();
        }

        public FrameInfo Duplicate()
        {
            FrameInfo duplicate = new FrameInfo();

            duplicate.FrameNumber = FrameNumber;
            duplicate.BeginTime = BeginTime;
            duplicate.EndTime = EndTime;
            ThreadItems.ForEach(item => duplicate.ThreadItems.Add(item.Duplicate()));

            return duplicate;
        }
    }
}
