// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Stride.Engine.Mcp
{
    internal class GameThreadRequest
    {
        public Func<Game, object> Action { get; init; }
        public TaskCompletionSource<object> Completion { get; init; }
    }
}
