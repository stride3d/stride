// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Utility
{
    /// <summary>
    /// A report message.
    /// </summary>
    public class ReportMessage
    {
        #region Constants and Fields

        /// <summary>
        /// Type of the message.
        /// </summary>
        public ReportMessageLevel Level;

        /// <summary>
        /// Span and location attached to this message.
        /// </summary>
        public SourceSpan Span;

        /// <summary>
        /// The error code.
        /// </summary>
        public string Code;

        /// <summary>
        /// Text of the message.
        /// </summary>
        public string Text;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportMessage"/> class.
        /// </summary>
        public ReportMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportMessage"/> class.
        /// </summary>
        /// <param name="level">The type.</param>
        /// <param name="code">The error code.</param>
        /// <param name="text">The text.</param>
        /// <param name="span">The span.</param>
        public ReportMessage(ReportMessageLevel level, string code, string text, SourceSpan span)
        {
            this.Level = level;
            this.Code = code;
            this.Text = text;
            this.Span = span;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: {1} {2} : {3}", this.Span, this.Level.ToString().ToLowerInvariant(), this.Code, this.Text);
        }

        #endregion
    }
}
