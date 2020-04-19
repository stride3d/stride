// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Text;

namespace Stride.Core.Shaders.Ast.Glsl
{
    /// <summary>
    /// Describe a register location
    /// </summary>
    public partial class LayoutQualifier : Qualifier
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutQualifier"/> class.
        /// </summary>
        public LayoutQualifier() : base("layout")
        {
            Layouts = new List<LayoutKeyValue>();
            IsPost = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutQualifier"/> class.
        /// </summary>
        /// <param name="layouts">The layouts.</param>
        public LayoutQualifier(params LayoutKeyValue[] layouts) : this()
        {
            Layouts.AddRange(layouts);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the profile.
        /// </summary>
        /// <value>
        ///   The profile.
        /// </value>
        public List<LayoutKeyValue> Layouts { get; set; }

        #endregion

        #region Public Methods

       

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            return Layouts;
        }

        /// <inheritdoc />
        public override string DisplayName
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append("layout(");
                for (int i = 0; i < Layouts.Count; i++)
                {
                    if (i > 0) builder.Append(", ");
                    builder.Append(Layouts[i]);
                }
                builder.Append(")");
                return builder.ToString();
            }
        }

        #endregion
    }
}
