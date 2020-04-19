// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Diagnostics;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Some parameters that can be transmitted from caller
    /// </summary>
    public class SerializerContextSettings
    {
        public static readonly SerializerContextSettings Default = new SerializerContextSettings();

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerContextSettings"/> class.
        /// </summary>
        public SerializerContextSettings()
        {
            MemberMask = DataMemberAttribute.DefaultMask;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerContextSettings"/> class.
        /// </summary>
        /// <param name="logger">The logger to use during serialization.</param>
        public SerializerContextSettings(ILogger logger)
            : this()
        {
            Logger = logger;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the member mask that will be used to filter <see cref="DataMemberAttribute.Mask"/>.
        /// </summary>
        /// <value>
        /// The member mask.
        /// </value>
        public uint MemberMask { get; set; }

        /// <summary>
        /// Gets or sets a property container to provide to the <see cref="SerializerContext.Properties"/>.
        /// </summary>
        public PropertyContainer Properties;
    }
}
