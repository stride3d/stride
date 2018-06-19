// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Rendering
{
    /// <summary>
    /// Defines how a <see cref="RenderObject"/> gets assigned to specific <see cref="RenderStage"/>.
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class RenderStageSelector
    {
        public abstract void Process(RenderObject renderObject);
    }
}
