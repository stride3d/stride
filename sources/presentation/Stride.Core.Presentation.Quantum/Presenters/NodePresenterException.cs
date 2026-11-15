// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Quantum.Presenters;

public class NodePresenterException : Exception
{
    public NodePresenterException(string? message)
        : base(message)
    {
    }

    public NodePresenterException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
