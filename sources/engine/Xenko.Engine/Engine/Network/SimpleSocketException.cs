// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Engine.Network
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
