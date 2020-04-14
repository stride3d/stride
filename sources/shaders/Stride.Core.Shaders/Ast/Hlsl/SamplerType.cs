// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A State type.
    /// </summary>
    public static class SamplerType
    {
        /// <summary>
        /// A sampler.
        /// </summary>
        public static readonly ObjectType Sampler = new ObjectType("sampler");

        /// <summary>
        /// A sampler1D.
        /// </summary>
        public static readonly ObjectType Sampler1D = new ObjectType("sampler1D");

        /// <summary>
        /// A sampler2D
        /// </summary>
        public static readonly ObjectType Sampler2D = new ObjectType("sampler2D");

        /// <summary>
        /// A sampler3D.
        /// </summary>
        public static readonly ObjectType Sampler3D = new ObjectType("sampler3D");

        /// <summary>
        /// A samplerCUBE.
        /// </summary>
        public static readonly ObjectType SamplerCube = new ObjectType("samplerCUBE");


        private static readonly ObjectType[] ObjectTypes = new[] { Sampler, Sampler1D, Sampler2D, Sampler3D, SamplerCube };

        public static bool IsSamplerType(this TypeBase type)
        {
            return Parse(type.Name) != null;
        }
        
        /// <summary>
        /// Parses the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static ObjectType Parse(string name)
        {
            foreach (var stateType in ObjectTypes)
            {
                if (string.Compare(name, stateType.Name.Text, StringComparison.OrdinalIgnoreCase) == 0)
                    return stateType;
            }
            return null;
        }
    }
}
