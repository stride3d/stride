// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Globalization;
using System.Linq;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A State type.
    /// </summary>
    public static class StreamTypeName
    {
        #region Constants and Fields

        /// <summary>
        /// A PointStream
        /// </summary>
        public static readonly ObjectType PointStream = new ObjectType("PointStream");

        /// <summary>
        /// A LineStream.
        /// </summary>
        public static readonly ObjectType LineStream = new ObjectType("LineStream");

        /// <summary>
        /// A TriangleStream.
        /// </summary>
        public static readonly ObjectType TriangleStream = new ObjectType("TriangleStream");

        private static readonly ObjectType[] StreamTypesName = new[] { PointStream, LineStream, TriangleStream };

        #endregion

        public static bool IsStreamTypeName(this TypeBase type)
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
            return StreamTypesName.FirstOrDefault(streamType =>  CultureInfo.InvariantCulture.CompareInfo.Compare(name, streamType.Name.Text, CompareOptions.None) == 0);
        }
    }
}
