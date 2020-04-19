// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Ast.Stride
{
    /// <summary>
    /// A State type.
    /// </summary>
    public static class StreamsType
    {
        #region Constants and Fields

        /// <summary>
        /// The constants streams.
        /// </summary>
        public static readonly ObjectType Constants = new ObjectType("Constants");

        /// <summary>
        /// The Input streams.
        /// </summary>
        public static readonly ObjectType Input = new ObjectType("Input");

        /// <summary>
        /// The Input2 type streams.
        /// </summary>
        public static readonly ObjectType Input2 = new ObjectType("Input2");

        /// <summary>
        /// The output type streams.
        /// </summary>
        public static readonly ObjectType Output = new ObjectType("Output");

        /// <summary>
        /// The streams type streams.
        /// </summary>
        public static readonly ObjectType Streams = new ObjectType("Streams");

        /// <summary>
        /// A fake variable of custom type stream
        /// </summary>
        public static readonly Variable ThisStreams = new Variable(Streams, "streams");


        /// <summary>
        /// Gets the streams.
        /// </summary>
        /// <returns>IEnumerable&lt;ObjectType&gt;.</returns>
        public static IEnumerable<ObjectType> GetStreams()
        {
            return AllTypes;
        }

        private static readonly ObjectType[] AllTypes = new[] { Constants, Input, Input2, Output, Streams};

        #endregion
        
        public static bool IsStreamsMutable(this TypeBase type)
        {
            return type != null && IsStreamsType(type) && type.Name != Constants;
        }

        public static bool IsStreamsType(this TypeBase type)
        {
            return type != null && Parse(type.Name) != null;
        }

        /// <summary>
        /// Parses the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static ObjectType Parse(string name)
        {
            foreach (var type in AllTypes)
            {
                if (CultureInfo.InvariantCulture.CompareInfo.Compare(name, type.Name.Text, CompareOptions.None) == 0)
                    return type;
            }
            return null;
        }
    }
}
