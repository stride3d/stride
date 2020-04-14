// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Core.Reflection
{
    /// <summary>
    /// Interface of a factory that can create instances of a type.
    /// </summary>
    public interface IObjectFactory
    {
        /// <summary>
        /// Creates a new instance of a type.
        /// </summary>
        /// <param name="type">The type of the instance to create.</param>
        /// <returns>A new default instance of a type.</returns>
        object New(Type type);
    }
}
