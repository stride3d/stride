// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// This class is used to store some properties of an exception. It is serializable.
    /// </summary>
    [DataContract, Serializable]
    public sealed class ExceptionInfo
    {
        private static readonly ExceptionInfo[] EmptyExceptions = new ExceptionInfo[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionInfo"/> class with default values for its properties
        /// </summary>
        public ExceptionInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionInfo"/> class from an <see cref="Exception"/>.
        /// </summary>
        /// <param name="exception">The exception used to initialize the properties of this instance.</param>
        public ExceptionInfo([NotNull] Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            Message = exception.Message;
            StackTrace = exception.StackTrace;
            TypeFullName = exception.GetType().FullName;
            TypeName = exception.GetType().Name;

            if (exception.InnerException != null)
            {
                InnerExceptions = new ExceptionInfo[] { new ExceptionInfo(exception.InnerException) };
            }
            else if (exception is ReflectionTypeLoadException reflectionException)
            {
                InnerExceptions = reflectionException.LoaderExceptions.Select(x => new ExceptionInfo(x)).ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the message of the exception.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the stack trace of the exception.
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the full name of the exception type. Should correspond to the <see cref="Type.FullName"/> property of the exception type.
        /// </summary>
        public string TypeFullName { get; set; }

        /// <summary>
        /// Gets or sets the name of the exception type. Should correspond to the <see cref="Type.Name"/> property of the exception type.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ExceptionInfo"/> of the inner exception.
        /// </summary>
        public ExceptionInfo[] InnerExceptions { get; set; } = EmptyExceptions;

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(TypeName))
            {
                sb.Append(TypeName);
            }

            if (!string.IsNullOrEmpty(Message))
            {
                if(sb.Length != 0)
                    sb.Append(": ");
                sb.Append(Message);
            }

            foreach(var child in InnerExceptions)
            {
                sb.AppendFormat("{0} ---> {1}", Environment.NewLine, child.ToString());
            }

            if (StackTrace != null)
            {
                sb.AppendLine();
                sb.Append(StackTrace);
            }

            return sb.ToString();
        }
    }
}
