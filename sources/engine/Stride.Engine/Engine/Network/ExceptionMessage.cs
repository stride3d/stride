// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Diagnostics;

namespace Stride.Engine.Network
{
    /// <summary>
    /// In the case of a SocketMessage when we use it in a SendReceiveAsync we want to propagate exceptions from the remote host
    /// </summary>
    public class ExceptionMessage : SocketMessage
    {
        /// <summary>
        /// Remote exception information
        /// </summary>
        public ExceptionInfo ExceptionInfo;
    }
}
