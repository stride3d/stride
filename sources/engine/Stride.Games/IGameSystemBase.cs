// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
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

using Stride.Core;

namespace Stride.Games
{
    /// <summary>
    ///   Interface for a component that represents a <strong>Game System</strong>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     A Game System is a component that can be used to manage game logic, resources, or other systems within the Game.
    ///     For example, it can be used to manage audio, input, or other subsystems.
    ///   </para>
    ///   <para>
    ///     Game Systems are typically added to the <see cref="GameBase"/> class and are initialized when the game starts.
    ///     If the Game System needs to be updated or drawn, it can implement the <see cref="IUpdateable"/> and <see cref="IDrawable"/>
    ///     interfaces respectively.
    ///   </para>
    /// </remarks>
    public interface IGameSystemBase : IComponent
    {
        /// <summary>
        ///   Initializes the Game System.
        /// </summary>
        /// <remarks>
        ///   This method is called when the component is added to the Game.
        ///   This can be used for tasks like querying for services the component needs
        ///   and setting up non-graphics resources (as here the Graphics Device may have not been initialized yet).
        /// </remarks>
        void Initialize();
    }
}
