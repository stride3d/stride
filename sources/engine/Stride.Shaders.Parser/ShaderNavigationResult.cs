// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Shaders.Utility;

namespace Stride.Shaders.Parser
{
    /// <summary>
    /// Results of a <see cref="ShaderNavigation"/>
    /// </summary>
    public class ShaderNavigationResult
    {
        public ShaderNavigationResult()
        {
            Messages = new LoggerResult();
        }

        /// <summary>
        /// Gets or sets the definition location.
        /// </summary>
        /// <value>The definition location.</value>
        public Stride.Core.Shaders.Ast.SourceSpan DefinitionLocation { get; set; }

        /// <summary>
        /// Gets the parsing messages.
        /// </summary>
        /// <value>The messages.</value>
        public LoggerResult Messages { get; set; }
    }
}
