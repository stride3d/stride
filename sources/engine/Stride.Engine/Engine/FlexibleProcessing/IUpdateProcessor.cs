// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Games;

namespace Stride.Engine.FlexibleProcessing
{
    public interface IUpdateProcessor
    {
        /// <summary>
        /// The order of the processor, smaller values execute first
        /// </summary>
        /// <remarks> Only evaluated once after instantiation </remarks>
        int Order { get; }

        void Update(GameTime gameTime);
    }

}
