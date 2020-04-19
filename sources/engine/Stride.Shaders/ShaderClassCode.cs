// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Shaders
{
    /// <summary>
    /// A common base class for shader classes with source code.
    /// </summary>
    [DataContract("ShaderClassCode")]
    public abstract class ShaderClassCode : ShaderSource
    {
        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        [DataMember(10)]
        public string ClassName { get; set; }

        /// <summary>
        /// Gets the generic parameters.
        /// </summary>
        /// <value>The generic parameters.</value>
        [DefaultValue(null), DataStyle(DataStyle.Compact)]
        [DataMember(20)]
        public string[] GenericArguments { get; set; }

        [DefaultValue(null)]
        [DataMember(30)]
        public Dictionary<string, string> GenericParametersArguments { get; set; }

        /// <summary>
        /// Returns a class name as a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A class name as a <see cref="System.String" /> that represents this instance.</returns>
        public string ToClassName()
        {
            if (GenericArguments == null)
                return ClassName;

            var result = new StringBuilder();
            result.Append(ClassName);
            if (GenericArguments != null && GenericArguments.Length > 0)
            {
                result.Append('<');
                result.Append(string.Join(",", GenericArguments));
                result.Append('>');
            }

            return result.ToString();
        }
        
        public override string ToString()
        {
            return ToClassName();
        }
    }
}
