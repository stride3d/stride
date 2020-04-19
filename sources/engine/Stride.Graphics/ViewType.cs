// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
namespace Stride.Graphics
{
    /// <summary>
    /// Defines how a view is selected from a resource.
    /// </summary>
    /// <remarks>
    /// This selection model is taken from Nuaj by Patapom (http://wiki.patapom.com/index.php/Nuaj)
    /// </remarks>
    public enum ViewType
    {
        /// <summary>
        /// Gets a texture view for the whole texture for all mips/arrays dimensions.
        /// </summary>
        /// <example>Here is what the view covers with whatever mipLevelIndex/arrayIndex
        /// 
        ///        Array0 Array1 Array2
        ///       ______________________
        ///  Mip0 |   X  |   X  |   X  |
        ///       |------+------+------|
        ///  Mip1 |   X  |   X  |   X  |
        ///       |------+------+------|
        ///  Mip2 |   X  |   X  |   X  |
        ///       ----------------------
        /// </example>
        Full = 0,

        /// <summary>
        /// Gets a single texture view at the specified index in the mip hierarchy and in the array of textures
        /// The texture view contains a single texture element at the specified mip level and array index
        /// </summary>
        /// <example>Here is what the view covers with mipLevelIndex=1 and mrrayIndex=1
        /// 
        ///        Array0 Array1 Array2
        ///       ______________________
        ///  Mip0 |      |      |      |
        ///       |------+------+------|
        ///  Mip1 |      |  X   |      |
        ///       |------+------+------|
        ///  Mip2 |      |      |      |
        ///       ----------------------
        /// </example>
        Single = 1,

        /// <summary>
        /// Gets a band texture view at the specified index in the mip hierarchy and in the array of textures
        /// The texture view contains all the mip level texture elements from the specified mip level and array index
        /// </summary>
        /// <example>Here is what the view covers with mipLevelIndex=1 and mrrayIndex=1
        /// 
        ///        Array0 Array1 Array2
        ///       ______________________
        ///  Mip0 |      |      |      |
        ///       |------+------+------|
        ///  Mip1 |      |  X   |      |
        ///       |------+------+------|
        ///  Mip2 |      |  X   |      |
        ///       ----------------------
        /// </example>
        ArrayBand = 2,

        /// <summary>
        /// Gets a band texture view at the specified index in the mip hierarchy and in the array of textures
        /// The texture view contains all the array texture elements from the specified mip level and array index
        /// </summary>
        /// <example>Here is what the view covers with mipLevelIndex=1 and mrrayIndex=1
        /// 
        ///        Array0 Array1 Array2
        ///       ______________________
        ///  Mip0 |      |      |      |
        ///       |------+------+------|
        ///  Mip1 |      |  X   |  X   |
        ///       |------+------+------|
        ///  Mip2 |      |      |      |
        ///       ----------------------
        /// </example>
        MipBand = 3,
    }
}
