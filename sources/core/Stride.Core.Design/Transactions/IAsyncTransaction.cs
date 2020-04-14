// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Transactions
{
    /// <summary>
    /// An interface representing an asynchronous transaction. An asynchronous transaction is a transaction that can be completed asynchronously. It
    /// provides additional safety such as preventing another asynchronous transaction to be created when there is one already in progress.
    /// </summary>
    public interface IAsyncTransaction
    {
    }
}
