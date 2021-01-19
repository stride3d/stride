// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Games.Time;
using Stride.Graphics;

namespace Stride.Games
{
    public interface IGameBaseInternal
    {
        EventHandler<GameUnhandledExceptionEventArgs> UnhandledExceptionInternal { get; }

        bool ForceOneUpdatePerDraw { get; set; }
        void InitializeBeforeRun();
        void LoadContentInternal();

    }
}
