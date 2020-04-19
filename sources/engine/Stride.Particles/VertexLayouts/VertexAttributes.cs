// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Particles.VertexLayouts
{
    /// <summary>
    /// A list of common <see cref="AttributeDescription"/> used to access the vertex fileds in a <see cref="ParticleVertexBuilder"/>
    /// </summary>
    public static class VertexAttributes
    {
        public static AttributeDescription Position = new AttributeDescription("POSITION");

        public static AttributeDescription Color = new AttributeDescription("COLOR");

        public static AttributeDescription Lifetime = new AttributeDescription("BATCH_LIFETIME");

        public static AttributeDescription RandomSeed = new AttributeDescription("BATCH_RANDOMSEED");
    }

}
