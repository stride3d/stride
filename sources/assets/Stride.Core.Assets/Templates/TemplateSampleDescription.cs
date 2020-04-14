// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;

namespace Stride.Core.Assets.Templates
{
    /// <summary>
    /// A template for using an existing package as a template, expecting a <see cref="Package"/> to be accessible 
    /// from <see cref="TemplateDescription.FullPath"/> with the same name as this template.
    /// </summary>
    [DataContract("TemplateSample")]
    public class TemplateSampleDescription : TemplateDescription
    {
        /// <summary>
        /// Gets or sets the name of the pattern used to substitute files and content. If null, use the 
        /// <see cref="TemplateDescription.DefaultOutputName"/>.
        /// </summary>
        /// <value>The name of the pattern.</value>
        [DataMember(70)]
        [DefaultValue(null)]
        public string PatternName { get; set; }
    }
}
