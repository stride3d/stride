// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Convertor
{
    /// <summary>
    /// A single map rule.
    /// </summary>
    public class VariableLayoutRule
    {
        /// <summary>
        /// Gets or sets from name.
        /// </summary>
        /// <value>
        /// From name.
        /// </value>
        public string Semantic { get; set; }

        /// <summary>
        /// Gets or sets to name.
        /// </summary>
        /// <value>
        /// To name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name output.
        /// </summary>
        /// <value>
        /// The name output.
        /// </value>
        public string NameOutput { get; set; }
        
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public string Location { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("Semantic: {0}, Name: {1}, NameOutput: {2} Location: {3}", Semantic, Name, NameOutput, Location);
        }
    }
}
