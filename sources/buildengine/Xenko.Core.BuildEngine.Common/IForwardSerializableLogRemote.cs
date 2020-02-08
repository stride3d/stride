// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Diagnostics;

namespace Xenko.Core.BuildEngine
{
    public interface IForwardSerializableLogRemote
    {
        void ForwardSerializableLog(SerializableLogMessage message);
    }
}
