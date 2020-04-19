// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Graphics
{
    /// <summary>
    /// Defines sprite sort-rendering options. 
    /// </summary>
    /// <remarks>
    /// Description is taken from original XNA <a href='http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.graphics.spritesortmode.aspx'>SpriteBatch</a> class.
    /// </remarks>
    public enum SpriteSortMode
    {
        /// <summary>
        /// Sprites are not drawn until End is called. 
        /// End will apply graphics device settings and draw all the sprites in one batch, in the same order calls to Draw were received. 
        /// This mode allows Draw calls to two or more instances of SpriteBatch without introducing conflicting graphics device settings. SpriteBatch defaults to Deferred mode.
        /// </summary>
        Deferred,

        /// <summary>
        /// Begin will apply new graphics device settings, and sprites will be drawn within each Draw call. In Immediate mode there can only be one active SpriteBatch instance without introducing conflicting device settings. 
        /// </summary>
        Immediate,

        /// <summary>
        /// Same as Deferred mode, except sprites are sorted by texture prior to drawing. This can improve performance when drawing non-overlapping sprites of uniform depth.
        /// </summary>
        Texture,

        /// <summary>
        /// Same as Deferred mode, except sprites are sorted by depth in back-to-front order prior to drawing. This procedure is recommended when drawing transparent sprites of varying depths.
        /// </summary>
        BackToFront,

        /// <summary>
        /// Same as Deferred mode, except sprites are sorted by depth in front-to-back order prior to drawing. This procedure is recommended when drawing opaque sprites of varying depths.
        /// </summary>
        FrontToBack,
    }
}
