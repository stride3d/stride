// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.UI.Events
{
    /// <summary>
    /// Represents and identifies a routed event and declares its characteristics.
    /// </summary>
    public abstract class RoutedEvent
    {
        /// <summary>
        /// Gets the identifying name of the routed event.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the registered owner type of the routed event.
        /// </summary>
        public Type OwnerType { get; internal set; }

        /// <summary>
        /// Gets the routing strategy of the routed event.
        /// </summary>
        public RoutingStrategy RoutingStrategy { get; internal set; }

        internal abstract Type HandlerSecondArgumentType { get; }

        internal RoutedEvent()
        {
        }
    }

    /// <summary>
    /// A routed event typed with the <see cref="RoutedEventArgs"/> it triggers.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="RoutedEventArgs"/> the routed event triggers</typeparam>
    public sealed class RoutedEvent<T> : RoutedEvent where T : RoutedEventArgs
    {
        internal override Type HandlerSecondArgumentType => typeof(T);
    }
}
