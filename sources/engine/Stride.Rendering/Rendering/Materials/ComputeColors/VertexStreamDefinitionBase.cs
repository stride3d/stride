// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// Base implementation for <see cref="IVertexStreamDefinition"/>
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class VertexStreamDefinitionBase : IVertexStreamDefinition
    {
        public abstract int GetSemanticNameHash();

        public abstract string GetSemanticName();
    }
}
