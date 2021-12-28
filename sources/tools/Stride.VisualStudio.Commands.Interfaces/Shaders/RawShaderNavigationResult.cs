// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.VisualStudio.Commands.Shaders
{
    /// <summary>
    /// Result of shader navigation.
    /// </summary>
    [Serializable]
    public class RawShaderNavigationResult
    {
        public RawShaderNavigationResult()
        {
            Messages = new List<RawShaderAnalysisMessage>();
        }

        /// <summary>
        /// Gets or sets the definition Span.
        /// </summary>
        /// <value>The definition Span.</value>
        public RawSourceSpan DefinitionSpan { get; set; }

        /// <summary>
        /// Gets the parsing messages.
        /// </summary>
        /// <value>The messages.</value>
        public List<RawShaderAnalysisMessage> Messages { get; private set; }
    }
}
