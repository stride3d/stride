// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Stride.Core.BuildEngine
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

    [ServiceContract]
    public interface IBuildMonitorRemote
    {
        [OperationContract]
        int Ping();

        [OperationContract(IsOneWay = true)]
        void StartBuild(Guid buildId, DateTime time);

        [OperationContract(IsOneWay = true)]
        [UseStrideDataContractSerializer]
        void SendBuildStepInfo(Guid buildId, long executionId, string description, DateTime startTime);

        [OperationContract(IsOneWay = true)]
        [UseStrideDataContractSerializer]
        void SendCommandLog(Guid buildId, DateTime startTime, long microthreadId, List<SerializableTimestampLogMessage> messages);

        [OperationContract(IsOneWay = true)]
        void SendMicrothreadEvents(Guid buildId, DateTime startTime, DateTime now, IEnumerable<MicrothreadNotification> microthreadJobInfo);

        [OperationContract(IsOneWay = true)]
        void SendBuildStepResult(Guid buildId, DateTime startTime, long microthreadId, ResultStatus status);

        [OperationContract(IsOneWay = true)]
        void EndBuild(Guid buildId, DateTime time);
    }
}
