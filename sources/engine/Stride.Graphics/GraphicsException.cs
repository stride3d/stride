// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Graphics
{
    public class GraphicsException : Exception
    {
        public GraphicsException()
        {
        }

        public GraphicsException(string message, GraphicsDeviceStatus status = GraphicsDeviceStatus.Normal)
            : base(message)
        {
            Status = status;
        }

        public GraphicsException(string message, Exception innerException, GraphicsDeviceStatus status = GraphicsDeviceStatus.Normal)
            : base(message, innerException)
        {
            Status = status;
        }

        public GraphicsDeviceStatus Status { get; private set; }
    }
}
