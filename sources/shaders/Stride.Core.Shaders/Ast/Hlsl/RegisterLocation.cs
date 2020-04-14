// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Describe a register location
    /// </summary>
    public partial class RegisterLocation : Qualifier
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "RegisterLocation" /> class.
        /// </summary>
        public RegisterLocation()
            : base("register")
        {
            IsPost = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterLocation"/> class.
        /// </summary>
        /// <param name="profile">
        /// The profile.
        /// </param>
        /// <param name="idenfitier">
        /// The idenfitier.
        /// </param>
        public RegisterLocation(Identifier profile, Identifier idenfitier)
            : base("register")
        {
            Profile = profile;
            Register = idenfitier;
            IsPost = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterLocation"/> class.
        /// </summary>
        /// <param name="profile">
        /// The profile.
        /// </param>
        /// <param name="idenfitier">
        /// The idenfitier.
        /// </param>
        public RegisterLocation(string profile, Identifier idenfitier)
            : base("register")
        {
            Profile = new Identifier(profile);
            Register = idenfitier;
            IsPost = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the profile.
        /// </summary>
        /// <value>
        ///   The profile.
        /// </value>
        public Identifier Profile { get; set; }

        /// <summary>
        ///   Gets or sets the register.
        /// </summary>
        /// <value>
        ///   The register.
        /// </value>
        public Identifier Register { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether the specified <see cref="RegisterLocation"/> is equal to this instance.
        /// </summary>
        /// <param name="other">
        /// The <see cref="RegisterLocation"/> to compare with this instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="RegisterLocation"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(RegisterLocation other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) && Equals(other.Profile, Profile) && Equals(other.Register, Register);
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

            return Equals(obj as RegisterLocation);
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            if (Profile != null)
            {
                ChildrenList.Add(Profile);
            }

            ChildrenList.Add(Register);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ (Profile != null ? Profile.GetHashCode() : 0);
                result = (result * 397) ^ (Register != null ? Register.GetHashCode() : 0);
                return result;
            }
        }

        /// <inheritdoc />
        public override string DisplayName
        {
            get
            {
                return Profile == null ? string.Format(": register({0})", Register) : string.Format(": register({0},{1})", Profile, Register);
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
        public static bool operator ==(RegisterLocation left, RegisterLocation right)
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
        public static bool operator !=(RegisterLocation left, RegisterLocation right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
