// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Engine
{
    /// <summary>
    /// Performs some work after world matrix has been updated.
    /// </summary>
    public abstract class TransformOperation
    {
        public abstract void Process(TransformComponent transformComponent);
    }
}
