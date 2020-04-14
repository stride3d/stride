// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Engine;
using Xenko.Graphics;

namespace Xenko.Rendering.Sprites
{
    /// <summary>
    /// A sprite provider from a <see cref="SpriteSheet"/>
    /// </summary>
    [DataContract("SpriteFromSheet")]
    [Display("Sprite Group")]
    public class SpriteFromSheet : IAnimatableSpriteProvider
    {
        /// <summary>
        /// Gets or sets the <see cref="Sheet"/> of the provider.
        /// </summary>
        /// <userdoc>The sheet that provides the sprites</userdoc>
        [DataMember]
        [InlineProperty(Expand = ExpandRule.Always)]
        public SpriteSheet Sheet { get; set; }

        /// <summary>
        /// Gets or sets the current frame of the animation.
        /// </summary>
        /// <userdoc>The index of the default frame of the sprite sheet to use.</userdoc>
        [DataMember]
        [DefaultValue(0)]
        [Display("Default Frame")]
        public int CurrentFrame { get; set; }

        /// <inheritdoc/>
        public int SpritesCount => Sheet?.Sprites.Count ?? 0;

        /// <summary>
        /// Creates a new instance of <see cref="SpriteFromSheet"/> with the specified <see cref="SpriteSheet"/>.
        /// <see cref="CurrentFrame"/> is initialized according to the specified <paramref name="spriteName"/>.
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="spriteName">The name of the sprite.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">sheet</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">No sprite in the sheet has the given name.</exception>
        /// <remarks>If two sprites have the provided name then the first sprite found is used.</remarks>
        public static SpriteFromSheet Create(SpriteSheet sheet, string spriteName)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            return new SpriteFromSheet
            {
                Sheet = sheet,
                CurrentFrame = sheet.FindImageIndex(spriteName),
            };
        }

        /// <inheritdoc/>
        public Sprite GetSprite()
        {
            var count = SpritesCount;
            return count > 0 ? Sheet.Sprites[(CurrentFrame % count + count) % count] : null; // in case of a negative index, it will cycle around
        }
    }
}
