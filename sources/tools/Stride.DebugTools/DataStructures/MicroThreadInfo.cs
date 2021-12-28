// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Framework.MicroThreading;

namespace Stride.DebugTools.DataStructures
{
    public class MicroThreadInfo
    {
        public long Id { get; set; }
        public MicroThreadState BeginState { get; set; }
        public MicroThreadState EndState { get; set; }
        public double BeginTime { get; set; }
        public double EndTime { get; set; }

        public MicroThreadInfo Duplicate()
        {
            MicroThreadInfo duplicate = new MicroThreadInfo();

            duplicate.Id = Id;
            duplicate.BeginState = BeginState;
            duplicate.EndState = EndState;
            duplicate.BeginTime = BeginTime;
            duplicate.EndTime = EndTime;

            return duplicate;
        }
    }
}
