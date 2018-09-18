using System;
using System.Globalization;

namespace Xenko.Metrics
{
    /// <summary>
    /// Key of a metric.
    /// </summary>
    public class MetricKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricKey"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The unique identifier.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public MetricKey(string name, Guid guid)
        {
            if (name == null) throw new ArgumentNullException("name");
            Name = name;
            Guid = guid;
        }

        /// <summary>
        /// The name
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The unique identifier
        /// </summary>
        public readonly Guid Guid;

        /// <summary>
        /// Gets the type of the metric.
        /// </summary>
        /// <returns>Type.</returns>
        public virtual Type GetMetricType()
        {
            return typeof(object);
        }

        /// <summary>
        /// Covnerts a metric value to a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A string representation of the value being passed.</returns>
        public virtual string ValueToString(object value)
        {
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Typed version of <see cref="MetricKey"/>.
    /// </summary>
    /// <typeparam name="T">Type of the metric</typeparam>
    public class MetricKey<T> : MetricKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricKey" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The unique identifier.</param>
        public MetricKey(string name, Guid guid)
            : base(name, guid)
        {
        }

        public override Type GetMetricType()
        {
            return typeof(T);
        }
    }
}