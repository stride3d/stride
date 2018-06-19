// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Annotations;

namespace Xenko.Core.Transactions
{
    /// <summary>
    /// A static factory to create <see cref="ITransactionStack"/> instances.
    /// </summary>
    public static class TransactionStackFactory
    {
        [NotNull]
        public static ITransactionStack Create(int capacity)
        {
            return new TransactionStack(capacity);
        }
    }
}
