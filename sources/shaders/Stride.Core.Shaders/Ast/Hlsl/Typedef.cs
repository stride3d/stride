// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Typedef declaration.
    /// </summary>
    public partial class Typedef : TypeBase, IDeclaration
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Typedef" /> class.
        /// </summary>
        public Typedef() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Typedef"/> class.
        /// </summary>
        /// <param name="typeBase">
        /// The type base.
        /// </param>
        public Typedef(TypeBase typeBase)
        {
            Type = typeBase;
            Qualifiers = Qualifier.None;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the names.
        /// </summary>
        /// <value>
        ///   The names.
        /// </value>
        public List<Typedef> SubDeclarators { get; set; }

        /// <summary>
        ///   Gets or sets the type.
        /// </summary>
        /// <value>
        ///   The type.
        /// </value>
        public TypeBase Type { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is group.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is group; otherwise, <c>false</c>.
        /// </value>
        public bool IsGroup
        {
            get
            {
                return SubDeclarators != null && SubDeclarators.Count > 0;
            }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Type);
            if (IsGroup)
                ChildrenList.AddRange(SubDeclarators);
            return ChildrenList;
        }

        /// <inheritdoc/>
        public override TypeBase ResolveType()
        {
            var type = TypeInference.TargetType ?? Type;
            return type.ResolveType();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder();
            if (IsGroup)
            {
                for (int i = 0; i < SubDeclarators.Count; i++)
                {
                    var typedefDeclarator = SubDeclarators[i];
                    if (i > 0)
                        builder.Append(", ");
                    builder.Append(typedefDeclarator.Name);
                }
            }
            else
            {
                builder.Append(Name);
            }

            return string.Format("typedef{0} {1} {2}", Qualifiers, Type, builder);
        }

        #endregion

    }
}
