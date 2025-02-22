// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Transactions;

/// <summary>
/// An completed interface that cannot be modified anymore, but can be rollbacked or rollforwarded.
/// </summary>
public interface IReadOnlyTransaction
{
    /// <summary>
    /// Gets an unique identifier for the transaction.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the operations executed during the transaction.
    /// </summary>
    IReadOnlyList<Operation> Operations { get; }

    /// <summary>
    /// Gets the transaction flags.
    /// </summary>
    TransactionFlags Flags { get; }
}
