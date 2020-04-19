// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Engine
{
    /// <summary>
    /// Allows to declare that a component requires another component in order to run (used for <see cref="ScriptComponent"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireComponentAttribute : EntityComponentAttributeBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RequireComponentAttribute"/>.
        /// </summary>
        /// <param name="type">Type of the required <see cref="EntityComponent"/></param>
        public RequireComponentAttribute(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets the type of the required component (Must be an <see cref="EntityComponent"/>.
        /// </summary>
        public Type Type { get; }
    }
}
