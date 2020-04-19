// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Transactions
{
    /// <summary>
    /// An exception triggered when an invalid operation related to a transaction stack occurs.
    /// </summary>
    public class TransactionException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <seealso cref="TransactionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TransactionException(string message)
            : base(message)
        {
        }
    }
}
