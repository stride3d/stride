// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A member reference in the form {this}.{Name}
    /// </summary>
    public partial class MemberReferenceExpression : Expression
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "MemberReferenceExpression" /> class.
        /// </summary>
        public MemberReferenceExpression()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberReferenceExpression"/> class.
        /// </summary>
        /// <param name="this">The @this.</param>
        /// <param name="member">The member.</param>
        public MemberReferenceExpression(Expression @this, Identifier member)
        {
            Target = @this;
            Member = member;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberReferenceExpression"/> class.
        /// </summary>
        /// <param name="this">The @this.</param>
        /// <param name="member">The member.</param>
        public MemberReferenceExpression(Expression @this, string member)
        {
            Target = @this;
            Member = new Identifier(member);
        }


        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the member.
        /// </summary>
        /// <value>
        /// The member.
        /// </value>
        public Identifier Member { get; set; }

        /// <summary>
        ///   Gets or sets the this.
        /// </summary>
        /// <value>
        ///   The this.
        /// </value>
        public Expression Target { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Target);
            ChildrenList.Add(Member);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0}.{1}", Target, Member);
        }

        #endregion
    }
}
