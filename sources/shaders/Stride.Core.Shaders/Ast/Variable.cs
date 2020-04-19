// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A variable declaration.
    /// </summary>
    public partial class Variable : Node, IAttributes, IDeclaration, IQualifiers
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Variable" /> class.
        /// </summary>
        public Variable()
        {
            Attributes = new List<AttributeBase>();
            Qualifiers = Qualifier.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="initialValue">The initial value.</param>
        public Variable(TypeBase type, string name, Expression initialValue = null)
        {
            Type = type;
            Name = new Identifier(name);
            InitialValue = initialValue;
            Attributes = new List<AttributeBase>();
            Qualifiers = Qualifier.None;
        }

        #endregion

        #region Public Properties


        /// <inheritdoc />
        public List<AttributeBase> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the qualifiers.
        /// </summary>
        /// <value>
        /// The qualifiers.
        /// </value>
        public Qualifier Qualifiers { get; set; }

        /// <summary>
        ///   Gets or sets the type.
        /// </summary>
        /// <value>
        ///   The type.
        /// </value>
        public TypeBase Type { get; set; }

        /// <summary>
        ///   Gets or sets the initial value.
        /// </summary>
        /// <value>
        ///   The initial value.
        /// </value>
        public Expression InitialValue { get; set; }

        /// <summary>
        ///   Gets or sets the name.
        /// </summary>
        /// <value>
        ///   The name.
        /// </value>
        public Identifier Name { get; set; }

        /// <summary>
        /// Gets or sets the sub variables (used only for variable group)
        /// </summary>
        /// <value>
        /// The sub variables inside this group.
        /// </value>
        public List<Variable> SubVariables { get; set; }

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
                return SubVariables != null && SubVariables.Count > 0;
            }
        }
        
        /// <summary>
        /// Returns single variable instances.
        /// </summary>
        /// <returns>An enumeration of single variable instances</returns>
        public IEnumerable<Variable> Instances()
        {
            if (IsGroup)
            {
                foreach (var subVariable in SubVariables)
                {
                    yield return subVariable;
                }
            } 
            else
            {
                yield return this;
            }
        }

        /// <summary>
        /// Merges attributes and qualifiers from another variable.
        /// </summary>
        /// <param name="from">The variable to merge attribute from.</param>
        public void MergeFrom(Variable from)
        {
            Qualifiers |= from.Qualifiers;
            Attributes.AddRange(from.Attributes);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Type);
            ChildrenList.Add(Name);
            if (Qualifiers != Qualifier.None) ChildrenList.Add(Qualifiers);
            if (InitialValue != null) ChildrenList.Add(InitialValue);
            if (SubVariables != null)
                ChildrenList.AddRange(SubVariables);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                "{0}{1} {2}{3}{4}", 
                Qualifiers.ToString(false),
                Type, 
                Name, 
                Qualifiers.ToString(true), 
                InitialValue != null ? " = " + InitialValue : string.Empty);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            }
        }
        #endregion
    }
}
