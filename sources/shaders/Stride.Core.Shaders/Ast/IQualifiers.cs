// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Shaders.Ast
{
    /// <summary>
    /// Base interface for all node providing qualifiers.
    /// </summary>
    public interface IQualifiers
    {
        /// <summary>
        /// Gets or sets the qualifiers.
        /// </summary>
        /// <value>
        /// The qualifiers.
        /// </value>
        Qualifier Qualifiers { get; set; }
    }
}
