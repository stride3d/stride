// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Rendering;

namespace Stride.Engine.Design
{
    /// <summary>
    /// An attribute used to associate a default <see cref="IEntityComponentRenderProcessor"/> to an entity component.
    /// </summary>
    public class DefaultEntityComponentRendererAttribute : DynamicTypeAttributeBase
    {
        private readonly int order;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEntityComponentRendererAttribute"/> class.
        /// </summary>
        /// <param name="type">The type must derived from <see cref="IEntityComponentRenderProcessor"/>.</param>
        public DefaultEntityComponentRendererAttribute(Type type) : base(type)
        {
            order = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEntityComponentRendererAttribute" /> class.
        /// </summary>
        /// <param name="type">The type must derived from <see cref="IEntityComponentRenderProcessor" />.</param>
        /// <param name="order">The order.</param>
        public DefaultEntityComponentRendererAttribute(Type type, int order) : base(type)
        {
            this.order = order;
        }

        public int Order
        {
            get
            {
                return order;
            }
        }
    }
} 
