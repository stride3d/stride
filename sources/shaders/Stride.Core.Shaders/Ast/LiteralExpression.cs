// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A literal expression.
    /// </summary>
    public partial class LiteralExpression : Expression
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LiteralExpression" /> class.
        /// </summary>
        public LiteralExpression()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteralExpression"/> class.
        /// </summary>
        /// <param name="literal">
        /// The literal.
        /// </param>
        public LiteralExpression(Literal literal)
        {
            Literal = literal;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteralExpression"/> class.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        public LiteralExpression(object value)
        {
            Literal = new Literal(value);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the literal.
        /// </summary>
        /// <value>
        ///   The literal.
        /// </value>
        public Literal Literal { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [DataMemberIgnore]
        public string Text
        {
            get { return Literal != null ? Literal.Text : null; }
            set
            {
                if (Literal != null)
                {
                    Literal.Text = value;
                }
                else
                {
                    Literal = value == null ? null : new Literal() {Text = value};
                }
            }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [DataMemberIgnore]
        public object Value
        {
            get { return Literal != null ? Literal.Value : null; }
            set
            {
                if (Literal != null)
                {
                    Literal.Value = value;
                }
                else
                {
                    Literal = value == null ? null : new Literal(value);
                }
            }
        }

        #endregion

        #region Public Methods

        public bool Equals(LiteralExpression other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Literal, Literal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(LiteralExpression)) return false;
            return Equals((LiteralExpression)obj);
        }

        public override int GetHashCode()
        {
            return (Literal != null ? Literal.GetHashCode() : 0);
        }

        public static bool operator ==(LiteralExpression left, LiteralExpression right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LiteralExpression left, LiteralExpression right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Literal);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0}", Literal);
        }

        #endregion
    }
}
