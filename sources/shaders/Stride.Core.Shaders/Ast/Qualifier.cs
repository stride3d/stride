// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Text;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A Storage qualifier.
    /// </summary>
    public partial class Qualifier : CompositeEnum
    {
        #region Constants and Fields

        /// <summary>
        /// None Enum.
        /// </summary>
        public static readonly Qualifier None = new Qualifier(string.Empty);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Qualifier" /> class.
        /// </summary>
        public Qualifier()
            : base(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Qualifier"/> class.
        /// </summary>
        /// <param name="key">
        /// Name of the enum.
        /// </param>
        public Qualifier(object key)
            : base(key, true)
        {
        }
        #endregion

        #region Operators

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a post qualifier.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is a post qualifier; otherwise, <c>false</c>.
        /// </value>
        public bool IsPost { get; set; }

        /// <summary>
        ///   Implements the operator ==.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static Qualifier operator &(Qualifier left, Qualifier right)
        {
            return OperatorAnd(left, right);
        }

        /// <summary>
        ///   Implements the operator |.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static Qualifier operator |(Qualifier left, Qualifier right)
        {
            return OperatorOr(left, right);
        }

        /// <summary>
        ///   Implements the operator ^.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static Qualifier operator ^(Qualifier left, Qualifier right)
        {
            return OperatorXor(left, right);
        }

        public string ToString(bool isPost)
        {
            var strBuild = new StringBuilder();
            var str = ToString<Qualifier>(qualifier => qualifier.IsPost == isPost);
            if (!string.IsNullOrEmpty(str))
            {
                if (isPost)
                {
                    strBuild.Append(" ");
                    strBuild.Append(str);
                }
                else
                {
                    strBuild.Append(str);
                    strBuild.Append(" ");
                }
                return strBuild.ToString();
            }

            return string.Empty;
        }

        public override string ToString()
        {
            return ToString<Qualifier>(qualifier => true);
        }

        #endregion
    }
}
