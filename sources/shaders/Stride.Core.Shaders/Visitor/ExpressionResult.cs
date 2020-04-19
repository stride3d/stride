// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Utility;

namespace Stride.Core.Shaders.Visitor
{
    /// <summary>
    /// Result of an expression.
    /// </summary>
    public class ExpressionResult : LoggerResult
    {
        /// <summary>
        /// Gets or sets the result of an expression.
        /// </summary>
        /// <value>
        /// The result of an expression.
        /// </value>
        public double Value { get; set; }
    }
}
