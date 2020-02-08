// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Xenko.Core.BuildEngine
{
    public class MicrothreadNotification
    {
        public enum NotificationType
        {
            JobStarted,
            JobEnded,
        };

        public int ThreadId;
        public long MicrothreadId;
        public long MicrothreadJobInfoId;
        public long Time;
        public NotificationType Type;

        public MicrothreadNotification() { }

        public MicrothreadNotification(int threadId, long microthreadId, long microthreadJobId, long time, NotificationType type)
        {
            ThreadId = threadId;
            MicrothreadId = microthreadId;
            MicrothreadJobInfoId = microthreadJobId;
            Time = time;
            Type = type;
        }
    }
}
