// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Describes a semantic.
    /// </summary>
    public partial class Semantic : Qualifier
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Semantic" /> class.
        /// </summary>
        public Semantic()
            : base("semantic")
        {
            IsPost = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Semantic"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public Semantic(string name)
            : base("semantic")
        {
            Name = new Identifier(name);
            IsPost = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the name.
        /// </summary>
        /// <value>
        ///   The name.
        /// </value>
        public Identifier Name { get; set; }

        /// <summary>
        /// Gets the base name of a semantic (COLOR1 -> COLOR)
        /// </summary>
        /// <value>
        /// The base name of sematnic.
        /// </value>
        public string BaseName
        {
            get
            {
                var match = MatchSemanticName.Match(Name.Text);
                return match.Groups[1].Value;
            }
        }

        /// <summary>
        /// Parses the specified semantic.
        /// </summary>
        /// <param name="text">The semantic.</param>
        /// <returns>The base name and index. COLOR1 -> {COLOR, 1}</returns>
        public static KeyValuePair<string, int> Parse(string text)
        {
            var match = MatchSemanticName.Match(text);
            if (!match.Success)
                return new KeyValuePair<string, int>(text, 0);

            string baseName = match.Groups[1].Value;
            int value = 0;
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                value = int.Parse(match.Groups[2].Value);
            }

            return new KeyValuePair<string, int>(baseName, value);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Determines whether the specified <see cref = "Semantic" /> is equal to this instance.
        /// </summary>
        /// <param name = "other">The <see cref = "Semantic" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref = "Semantic" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// <inheritdoc />
        public bool Equals(Semantic other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) && Equals(other.Name, Name);
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

            return Equals(obj as Semantic);
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Name);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        /// <inheritdoc />
        public override string DisplayName
        {
            get
            {
                return string.Format(": {0}", Name);
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
        public static bool operator ==(Semantic left, Semantic right)
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
        public static bool operator !=(Semantic left, Semantic right)
        {
            return !Equals(left, right);
        }

        private static readonly Regex MatchSemanticName = new Regex(@"([A-Za-z_]+)(\d*)");

        #endregion
    }
}
