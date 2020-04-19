// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A state expresion in the form: sampler {...}.
    /// </summary>
    public partial class StateExpression : Expression
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "StateExpression" /> class.
        /// </summary>
        public StateExpression()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateExpression"/> class.
        /// </summary>
        /// <param name="stateType">
        /// Type of the state.
        /// </param>
        /// <param name="initializer">
        /// The initializer.
        /// </param>
        public StateExpression(TypeName stateType, StateInitializer initializer)
        {
            this.StateType = stateType;
            this.Initializer = initializer;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the initializer.
        /// </summary>
        /// <value>
        ///   The initializer.
        /// </value>
        public StateInitializer Initializer { get; set; }

        /// <summary>
        ///   Gets or sets the type of the sampler.
        /// </summary>
        /// <value>
        ///   The type of the sampler.
        /// </value>
        public TypeBase StateType { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(StateType);
            ChildrenList.Add(Initializer);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0} {1}", StateType, Initializer);
        }

        #endregion
    }
}
