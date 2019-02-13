// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Rendering.Compositing
{
    public interface ISharedRenderer : IIdentifiable, IGraphicsRendererBase
    {
        string Name { get; }
    }
}
