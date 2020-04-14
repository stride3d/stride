// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Convertor
{
    /// <summary>
    /// A map rule for types.
    /// </summary>
    public class MapRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("<map name='{0}' type='{1}'/>", this.Name, this.Type);
        }
    }
}
