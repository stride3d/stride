// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;


namespace Stride.ExecServer
{
    /// <summary>
    /// ServiceWire Interface used to log back standard output and error to client.
    /// </summary>
    public interface IServerLogger
    {
        void OnLog(string text, ConsoleColor color);
    }
}
