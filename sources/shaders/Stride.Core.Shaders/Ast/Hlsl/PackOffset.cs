// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Describes a packoffset(value).
    /// </summary>
    public partial class PackOffset : Qualifier
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "PackOffset" /> class.
        /// </summary>
        public PackOffset()
            : base("packoffset")
        {
            IsPost = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackOffset"/> class.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        public PackOffset(string value)
            : base("packoffset")
        {
            var identifier = new IdentifierDot();
            identifier.Identifiers.Add(new Identifier(value));
            Value = identifier;
            IsPost = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the value.
        /// </summary>
        /// <value>
        ///   The value.
        /// </value>
        public Identifier Value { get; set; }

        #endregion

        #region Public Methods


        private static readonly Regex matchComponent = new Regex(@"^c(\d+)(\.[xyzw])?$");

        /// <summary>
        /// Converts this packoffset to a register index based on float size.
        /// </summary>
        /// <returns>An offset </returns>
        public int ToFloat4SlotIndex()
        {
            var match = matchComponent.Match(Value.ToString());
            if (!match.Success)
                return -1;
            var index = int.Parse(match.Groups[1].Value) * 16;
            if (match.Groups[2].Success)
            {
                var subComponentChar = match.Groups[2].Value[1];
                index += "xyzw".IndexOf(subComponentChar)* 4;
            }
            return index;
        }

        /// <summary>
        /// Determines whether the specified <see cref="PackOffset"/> is equal to this instance.
        /// </summary>
        /// <param name="other">
        /// The <see cref="PackOffset"/> to compare with this instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="PackOffset"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(PackOffset other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) && Equals(other.Value, Value);
        }

        /// <inheritdoc />
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

            return Equals(obj as PackOffset);
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Value);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }

        /// <inheritdoc />
        public override string DisplayName
        {
            get
            {
                return string.Format(": packoffset({0})", Value);
            }
        }

        #endregion

        #region Operators

        /// <summary>
        ///   Implements the operator ==.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static bool operator ==(PackOffset left, PackOffset right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Implements the operator !=.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static bool operator !=(PackOffset left, PackOffset right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
