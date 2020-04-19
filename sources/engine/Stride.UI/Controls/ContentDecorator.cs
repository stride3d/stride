// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Engine;

namespace Stride.UI.Controls
{
    /// <summary>
    /// A <see cref="ContentControl"/> decorating its <see cref="ContentControl.Content"/> with a background image.
    /// </summary>
    [DataContract(nameof(ContentDecorator))]
    public class ContentDecorator : ContentControl
    {
        /// <summary>
        /// Gets or sets the background image.
        /// </summary>
        /// <userdoc>The background image.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider BackgroundImage { get; set; }

        public ContentDecorator()
        {
            DrawLayerNumber += 1; // (decorator design image)
        }
    }
}
