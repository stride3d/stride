// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A field of a struct.
    /// </summary>
    public sealed partial class Literal : Node
    {
        private object value;


        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Literal" /> class.
        /// </summary>
        public Literal()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Literal"/> class.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        public Literal(object value)
        {
            Value = value;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the value.
        /// </summary>
        /// <value>
        ///   The value.
        /// </value>
        public object Value
        {
            get
            {
                return value;
            }

            set
            {
                this.value = value;
                Text = ConvertValueToString(value);
            }
        }

        /// <summary>
        ///   Gets or sets the text.
        /// </summary>
        /// <value>
        ///   The text.
        /// </value>
        public string Text { get; set; }


        /// <summary>
        /// Gets or sets the sub literals.
        /// </summary>
        /// <value>
        /// The sub literals.
        /// </value>
        /// <remarks>
        /// This value can be null.
        /// </remarks>
        public List<Literal> SubLiterals { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            if (SubLiterals != null) ChildrenList.AddRange(SubLiterals);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var str = Text;
            if (SubLiterals != null) str = string.Join(" ", SubLiterals);
            return string.Format("{0}", str);
        }

        private static string ConvertValueToString(object value)
        {
            if (value is float)
            {
                string defaultString = ((float)value).ToString("g", CultureInfo.InvariantCulture);
                if (!defaultString.Contains(".") && !defaultString.Contains("e"))
                    defaultString += ".0";
                return defaultString;
            }
            if (value is double)
            {
                string defaultString = ((double)value).ToString("g", CultureInfo.InvariantCulture);
                if (!defaultString.Contains(".") && !defaultString.Contains("e"))
                    defaultString += ".0";
                return defaultString;
            }
            if (value is int)
                return ((int)value).ToString(CultureInfo.InvariantCulture);
            if (value is uint)
                return ((uint)value).ToString(CultureInfo.InvariantCulture);
            if (value is bool)
                return (bool)value ? "true" : "false";

            return value.ToString();            
        }

        /// <inheritdoc/>
        public bool Equals(Literal other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(other.value, value);
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
            if (obj.GetType() != typeof(Literal))
            {
                return false;
            }
            return Equals((Literal)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (value != null ? value.GetHashCode() : 0);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Literal left, Literal right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Literal left, Literal right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
