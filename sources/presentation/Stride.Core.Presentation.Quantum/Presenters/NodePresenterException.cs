// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    public class NodePresenterException : Exception
    {
        public NodePresenterException([NotNull] string message) : base(message)
        {
        }

        public NodePresenterException([NotNull] string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
