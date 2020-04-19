// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core
{
    /// <summary>
    /// Interface ICollectorHolder for an instance that can collect other instance.
    /// </summary>
    public interface ICollectorHolder
    {
        /// <summary>
        /// Gets the collector.
        /// </summary>
        /// <value>The collector.</value>
        ObjectCollector Collector { get; }
    }
}
