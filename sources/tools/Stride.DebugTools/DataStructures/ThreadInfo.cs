// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.DebugTools.DataStructures
{
    public class ThreadInfo
    {
        public int Id { get; set; }
        public List<MicroThreadInfo> MicroThreadItems { get; private set; }

        public ThreadInfo()
        {
            MicroThreadItems = new List<MicroThreadInfo>();
        }

        public ThreadInfo Duplicate()
        {
            ThreadInfo duplicate = new ThreadInfo();

            duplicate.Id = Id;
            MicroThreadItems.ForEach(item => duplicate.MicroThreadItems.Add(item.Duplicate()));

            return duplicate;
        }
    }
}
