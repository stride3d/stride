// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Engine.Network
{
    /// <summary>
    /// Used when there is a socket exception.
    /// </summary>
    public class SimpleSocketException : Exception
    {
        public SimpleSocketException(string message) : base(message)
        {
        }
    }
}
