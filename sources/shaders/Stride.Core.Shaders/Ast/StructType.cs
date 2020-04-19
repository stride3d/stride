// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A structure.
    /// </summary>
    public partial class StructType : TypeBase, IDeclaration, IScopeContainer
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "StructType" /> class.
        /// </summary>
        public StructType()
        {
            Fields = new List<Variable>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the fields.
        /// </summary>
        /// <value>
        ///   The fields.
        /// </value>
        public List<Variable> Fields { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Name);
            ChildrenList.AddRange(Fields);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("struct {0} {{...}}", Name);
        }

        /// <inheritdoc/>
        public bool Equals(StructType other)
        {
            return base.Equals(other);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return Equals(obj as StructType);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(StructType left, StructType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StructType left, StructType right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
