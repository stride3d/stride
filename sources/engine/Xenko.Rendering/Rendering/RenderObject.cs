// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Rendering
{
    /// <summary>
    /// Describes something that can be rendered by a <see cref="RootRenderFeature"/>.
    /// </summary>
    public abstract class RenderObject
    {
        private static int currentIndex;

        public bool Enabled = true;
        public RenderGroup RenderGroup;

        public BoundingBoxExt BoundingBox;

        // Kept in cache to quickly know if RenderPerFrameNode was already generated
        public RootRenderFeature RenderFeature;
        public ObjectNodeReference ObjectNode;
        public StaticObjectNodeReference StaticObjectNode = StaticObjectNodeReference.Invalid;

        public StaticObjectNodeReference VisibilityObjectNode = StaticObjectNodeReference.Invalid;

        public ActiveRenderStage[] ActiveRenderStages;
        public uint StateSortKey;
        public readonly int Index = Interlocked.Increment(ref currentIndex);

        // TODO: Switch to a "StaticPropertyContainer" that will be optimized by assembly processor
        //public PropertyContainer Tags;

        // TODO: We should probably switch to RenderData (but right now its local to either VisibilityGroup or RenderFeature) or the previously mentioned StaticPropertyContainer to have more flexibility.
        /// <summary>
        /// Can be used to setup a link to a source.
        /// Typically, this might be an entity component.
        /// </summary>
        public object Source;
    }
}
