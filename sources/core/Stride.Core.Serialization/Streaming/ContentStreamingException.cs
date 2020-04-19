// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Streaming
{
    /// <summary>
    /// The exception that is thrown when an internal error happened in the Audio System. That is an error that is not due to the user behavior.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public sealed class ContentStreamingException : Exception
    {
        /// <summary>
        /// Gets the storage container that causes this exception.
        /// </summary>
        public ContentStorage Storage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentStreamingException"/> class.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="storage">The storage container.</param>
        public ContentStreamingException(string msg, ContentStorage storage = null)
            : base("An internal error happened in the content streaming service [details:'" + msg + "'")
        {
            Storage = storage;
        }
    }
}
