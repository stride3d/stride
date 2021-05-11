// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Games
{
    public interface IMessageLoop : IDisposable
    {
        bool NextFrame();
    }
}
