// Copyright (c) 2017 Stride (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Metrics
{
    /// <summary>
    /// Identifies a metric application.
    /// </summary>
    public sealed class MetricAppId
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAppId"/> class.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public MetricAppId(Guid guid, string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            Guid = guid;
            Name = name;
        }

        /// <summary>
        /// The unique identifier of this application.
        /// </summary>
        public readonly Guid Guid;

        /// <summary>
        /// The name of this application.
        /// </summary>
        public readonly string Name;
    }
}