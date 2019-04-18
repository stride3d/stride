// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading;
using Xenko.Graphics;

namespace Xenko.Rendering
{
    public struct ResourceGroupEntry
    {
        public int LastFrameUsed;
        public ResourceGroup Resources;

        /// <summary>
        /// Mark resource group as used during this frame.
        /// </summary>
        /// <returns>True if state changed (object was not mark as used during this frame until now), otherwise false.</returns>
        public bool MarkAsUsed(RenderSystem renderSystem)
        {
            return Interlocked.Exchange(ref LastFrameUsed, renderSystem.FrameCounter) != renderSystem.FrameCounter;
        }
    }
}
