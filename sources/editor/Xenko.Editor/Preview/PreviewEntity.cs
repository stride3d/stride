// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Engine;

namespace Xenko.Editor.Preview
{
    public class PreviewEntity
    {
        /// <summary>
        /// The entity to preview.
        /// </summary>
        public Entity Entity;

        /// <summary>
        /// The actions to undertake when the preview entity is not used anymore.
        /// </summary>
        public Action Disposed;

        public PreviewEntity(Entity entity)
        {
            Entity = entity;
        }
    }
}
