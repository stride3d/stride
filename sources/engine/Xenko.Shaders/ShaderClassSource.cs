// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

using Xenko.Core;
using Xenko.Core.Serialization;

namespace Xenko.Shaders
{
    /// <summary>
    /// A shader class used for mixin.
    /// </summary>
    [DataContract("ShaderClassSource")]
    public sealed class ShaderClassSource : ShaderSource, IEquatable<ShaderClassSource>
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
        /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
        /// </summary>
        public ShaderClassSource()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        public ShaderClassSource(string className)
            : this(className, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="genericArguments">The generic parameters.</param>
        public ShaderClassSource(string className, params string[] genericArguments)
        {
            ClassName = className;
            GenericArguments = genericArguments;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="genericArguments">The generic parameters.</param>
        public ShaderClassSource(string className, params object[] genericArguments)
        {
            ClassName = className;
            if (genericArguments != null)
            {
                GenericArguments = new string[genericArguments.Length];
                for (int i = 0; i < genericArguments.Length; ++i)
                {
                    var genArg = genericArguments[i];
                    if (genArg is bool)
                        GenericArguments[i] = ((bool)genArg) ? "true" : "false";
                    else
                        GenericArguments[i] = genArg == null ? "null" : Convert.ToString(genArg, CultureInfo.InvariantCulture);
                }
            }
        }

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

        public bool Equals(ShaderClassSource shaderClassSource)
        {
            if (ReferenceEquals(null, shaderClassSource)) return false;
            if (ReferenceEquals(this, shaderClassSource)) return true;
            return string.Equals(ClassName, shaderClassSource.ClassName) && Utilities.Compare(GenericArguments, shaderClassSource.GenericArguments);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ShaderClassSource)obj);
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
            return new ShaderClassSource(ClassName, GenericArguments = GenericArguments != null ? GenericArguments.ToArray() : null);
        }
        
        public override string ToString()
        {
            return ToClassName();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="ShaderClassSource"/>.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator ShaderClassSource(string className)
        {
            return new ShaderClassSource(className);
        }
    }
}
