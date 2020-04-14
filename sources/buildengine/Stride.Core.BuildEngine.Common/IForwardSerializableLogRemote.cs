// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ServiceModel;

using Xenko.Core.Diagnostics;

namespace Xenko.Core.BuildEngine
{
    [ServiceContract]
    public interface IForwardSerializableLogRemote
    {
        [OperationContract(IsOneWay = true)]
        [UseXenkoDataContractSerializer]
        void ForwardSerializableLog(SerializableLogMessage message);
    }
}
