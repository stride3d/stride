// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A class that represents a copy of a <see cref="LogMessage"/> that can be serialized.
    /// </summary>
    [DataContract, Serializable]
    public class SerializableLogMessage : ILogMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableLogMessage"/> class with default values for its properties.
        /// </summary>
        public SerializableLogMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableLogMessage"/> class from a <see cref="LogMessage"/> instance.
        /// </summary>
        /// <param name="message">The <see cref="LogMessage"/> instance to use to initialize properties.</param>
        public SerializableLogMessage([NotNull] LogMessage message)
        {
            Module = message.Module;
            Type = message.Type;
            Text = message.Text;
            ExceptionInfo = message.Exception != null ? new ExceptionInfo(message.Exception) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableLogMessage"/> class using the given parameters to set its properties.
        /// </summary>
        /// <param name="module">The module name.</param>
        /// <param name="type">The type.</param>
        /// <param name="text">The text.</param>
        /// <param name="exceptionInfo">The exception information. This parameter can be null.</param>
        public SerializableLogMessage([NotNull] string module, LogMessageType type, [NotNull] string text, ExceptionInfo exceptionInfo = null)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (text == null) throw new ArgumentNullException(nameof(text));
            Module = module;
            Type = type;
            Text = text;
            ExceptionInfo = exceptionInfo;
        }

        /// <summary>
        /// Gets or sets the module.
        /// </summary>
        /// <remarks>
        /// The module is an identifier for a logical part of the system. It can be a class name, a namespace or a regular string not linked to a code hierarchy.
        /// </remarks>
        public string Module { get; set; }

        /// <summary>
        /// Gets or sets the type of this message.
        /// </summary>
        public LogMessageType Type { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ExceptionInfo"/> of this message.
        /// </summary>
        public ExceptionInfo ExceptionInfo { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{(Module != null ? $"[{Module}]: " : string.Empty)}{Type}: {Text}{(ExceptionInfo != null ? $". {ExceptionInfo.Message}" : string.Empty)}";
        }
    }
}
