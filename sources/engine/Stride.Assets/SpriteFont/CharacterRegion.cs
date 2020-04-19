// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;

namespace Stride.Assets.SpriteFont
{
    /// <summary>
    /// Describes a range of consecutive characters that should be included in the font.
    /// </summary>
    [DataContract("CharacterRegion")]
    public class CharacterRegion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterRegion"/> class.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public CharacterRegion(char start, char end)
            : this()
        {
            if (start > end)
                throw new ArgumentException();

            Start = start;
            End = end;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterRegion"/> class.
        /// </summary>
        public CharacterRegion()
        {
        }

        /// <summary>
        /// The first character to include in the region.
        /// </summary>
        /// <userdoc>
        /// The first character of the region.
        /// </userdoc>
        [DataMember(0)]
        public char Start;

        /// <summary>
        /// The second character to include in the region.
        /// </summary>
        /// <userdoc>
        /// The last character of the region.
        /// </userdoc>
        [DataMember(1)]
        public char End;

        // Flattens a list of character regions into a combined list of individual characters.
        public static IEnumerable<char> Flatten(List<CharacterRegion> regions)
        {
            if (regions.Any())
            {
                // If we have any regions, flatten them and remove duplicates.
                return regions.SelectMany(region => region.GetCharacters()).Distinct();
            }

            // If no regions were specified, use the default.
            return Default.GetCharacters();
        }

        // Default to just the base ASCII character set.
        public static CharacterRegion Default = new CharacterRegion(' ', '~');

        // Enumerates all characters within the region.
        private IEnumerable<char> GetCharacters()
        {
            for (char c = Start; c <= End; c++)
            {
                yield return c;
            }
        }
    }
}
