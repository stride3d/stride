// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Assets.Scripts
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
