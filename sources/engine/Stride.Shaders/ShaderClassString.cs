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
    /// A shader class based on source code string, used for mixin.
    /// </summary>
    [DataContract("ShaderClassString")]
    public sealed class ShaderClassString : ShaderClassCode, IEquatable<ShaderClassString>
    {
        /// <summary>
        /// Gets the source code of this shader class as string, XKSL syntax.
        /// </summary>
        /// <value>The source code of the shader class.</value>
        public string ShaderSourceCode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassString"/> class.
        /// </summary>
        public ShaderClassString()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassString"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        public ShaderClassString(string className, string shaderSourceCode)
            : this(className, shaderSourceCode, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassString"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="genericArguments">The generic parameters.</param>
        public ShaderClassString(string className, string shaderSourceCode, params string[] genericArguments)
        {
            ClassName = className;
            ShaderSourceCode = shaderSourceCode;
            GenericArguments = genericArguments;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassString"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="genericArguments">The generic parameters.</param>
        public ShaderClassString(string className, string shaderSourceCode, params object[] genericArguments)
        {
            ClassName = className;
            ShaderSourceCode = shaderSourceCode;

            if (genericArguments != null)
            {
                GenericArguments = new string[genericArguments.Length];
                for (int i = 0; i < genericArguments.Length; ++i)
                {
                    var genArg = genericArguments[i];
                    if (genArg is bool boolArg)
                        GenericArguments[i] = boolArg ? "true" : "false";
                    else
                        GenericArguments[i] = genArg == null ? "null" : Convert.ToString(genArg, CultureInfo.InvariantCulture);
                }
            }
        }

        public bool Equals(ShaderClassString shaderClassString)
        {
            if (ReferenceEquals(null, shaderClassString)) return false;
            if (ReferenceEquals(this, shaderClassString)) return true;
            return string.Equals(ClassName, shaderClassString.ClassName) && Utilities.Compare(GenericArguments, shaderClassString.GenericArguments);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ShaderClassString)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ClassName?.GetHashCode() ?? 0;
                if (GenericArguments != null)
                {
                    foreach (var current in GenericArguments)
                        hashCode = (hashCode * 397) ^ (current?.GetHashCode() ?? 0);
                }

                return hashCode;
            }
        }

        public override object Clone()
        {
            return new ShaderClassString(ClassName, ShaderSourceCode, GenericArguments = GenericArguments != null ? GenericArguments.ToArray() : null);
        }
        
        public override string ToString()
        {
            return ToClassName();
        }
    }
}
