// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A log message generate by <see cref="Profiler"/>.
    /// </summary>
    public class ProfilingMessage : LogMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingMessage" /> class.
        /// </summary>
        /// <param name="profileId">The profile unique identifier.</param>
        /// <param name="profilingKey">The profile key.</param>
        /// <param name="profilingType">Type of the profile.</param>
        public ProfilingMessage(int profileId, [NotNull] ProfilingKey profilingKey, ProfilingMessageType profilingType) : this(profileId, profilingKey, profilingType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingMessage" /> class.
        /// </summary>
        /// <param name="profileId">The profile unique identifier.</param>
        /// <param name="profilingKey">The profile key.</param>
        /// <param name="profilingType">Type of the profile.</param>
        /// <param name="text">The text.</param>
        public ProfilingMessage(int profileId, [NotNull] ProfilingKey profilingKey, ProfilingMessageType profilingType, string text)
            : base("Profiler", LogMessageType.Info, text)
        {
            if (profilingKey == null) throw new ArgumentNullException(nameof(profilingKey));

            Id = profileId;
            Key = profilingKey;
            ProfilingType = profilingType;
        }

        /// <summary>
        /// Gets or sets the unique identifier associated with this profile message.
        /// </summary>
        /// <value>The unique identifier.</value>
        public int Id { get; }

        /// <summary>
        /// Gets or sets the profile key.
        /// </summary>
        /// <value>The profile key.</value>
        public ProfilingKey Key { get; }

        /// <summary>
        /// Gets the type of the profile.
        /// </summary>
        /// <value>The type of the profile.</value>
        public ProfilingMessageType ProfilingType { get; }
 
        /// <summary>
        /// Gets or sets the time elapsed for this particular profile.
        /// </summary>
        /// <value>The elapsed.</value>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Gets attributes attached to this message. May be null.
        /// </summary>
        /// <value>The properties.</value>
        public Dictionary<object, object> Attributes { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder(Text != null ? $": {Text}" : string.Empty);
            var hasElapsed = false;
            if (ProfilingType != ProfilingMessageType.Begin)
            {
                builder.Append(": ");

                if (ElapsedTime > new TimeSpan(0, 0, 1, 0))
                {
                    builder.AppendFormat("Elapsed = {0:0.000}m", ElapsedTime.TotalMinutes);
                }
                else if (ElapsedTime > new TimeSpan(0, 0, 0, 0, 1000))
                {
                    builder.AppendFormat("Elapsed = {0:0.000}s", ElapsedTime.TotalSeconds);
                }
                else
                {
                    builder.AppendFormat("Elapsed = {0:0.000}ms", ElapsedTime.TotalMilliseconds);
                }
                hasElapsed = true;
            }

            if (Attributes != null && Attributes.Count > 0)
            {
                if (!hasElapsed)
                {
                    builder.Append(": ");
                }

                foreach (var keyValue in Attributes)
                {
                    builder.Append(", ").Append(keyValue.Key).Append(" = ").Append(keyValue.Value);
                }
            }

            return $"[{Module}] #{Id}: {ProfilingType}: {Key}{builder}";
        }
    }
}
