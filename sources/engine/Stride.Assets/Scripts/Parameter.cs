// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Assets.Scripts
{
    [DataContract]
    public class Parameter : Symbol
    {
        public Parameter()
        {
        }

        public Parameter(string type, string name) : base(type, name)
        {
        }

        /// <summary>
        /// Describes whether the parameter is passed by value or by reference.
        /// </summary>
        [DataMember(-40)]
        public ParameterRefKind RefKind { get; set; }

        public override string ToString()
        {
            var result = base.ToString();

            // Add ref kind
            if (RefKind != ParameterRefKind.None)
                result = RefKind.ToString().ToLowerInvariant() + " " + result;

            return result;
        }
    }
}
