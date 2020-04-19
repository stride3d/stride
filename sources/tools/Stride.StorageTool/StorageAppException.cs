// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.StorageTool
{
    /// <summary>
    /// Class StorageAppException.
    /// </summary>
    public class StorageAppException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAppException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public StorageAppException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAppException" /> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public StorageAppException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
