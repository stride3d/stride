// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core
{
    /// <summary>
    /// Indicates that a class can be serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DataContractAttribute : Attribute
    {
        private readonly string alias;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContractAttribute"/> class.
        /// </summary>
        public DataContractAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContractAttribute"/> class.
        /// </summary>
        /// <param name="aliasName">The type alias name when serializing to a textual format.</param>
        public DataContractAttribute(string aliasName)
        {
            this.alias = aliasName;
        }

        /// <summary>
        /// Gets or sets the alias name when serializing to a textual format.
        /// </summary>
        /// <value>The alias name.</value>
        public string Alias
        {
            get
            {
                return alias;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DataContractAttribute"/> is implicitly inherited by all its descendant classes.
        /// </summary>
        /// <value><c>true</c> if inherited; otherwise, <c>false</c>.</value>
        public bool Inherited { get; set; }

        /// <summary>
        /// The default member mode.
        /// </summary>
        public DataMemberMode DefaultMemberMode { get; set; } = DataMemberMode.Default;
    }
}
