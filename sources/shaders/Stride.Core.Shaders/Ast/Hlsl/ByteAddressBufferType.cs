// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    public static class ByteAddressBufferType
    {
        public static readonly ObjectType ByteAddressBuffer = new ObjectType("ByteAddressBuffer");

        public static readonly ObjectType RWByteAddressBuffer = new ObjectType("RWByteAddressBuffer");

        private static readonly ObjectType[] ObjectTypes = new[] { ByteAddressBuffer, RWByteAddressBuffer };

        public static bool IsByteAddressBufferType(this TypeBase type)
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
            foreach (var objectType in ObjectTypes)
            {
                if (string.Compare(name, objectType.Name.Text, StringComparison.OrdinalIgnoreCase) == 0)
                    return objectType;
            }
            return null;
        }
    }
}
