// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Rendering;
using Stride.Rendering.Sprites;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor in charge of updating and drawing the entities having background components.
    /// </summary>
    internal class BackgroundComponentProcessor : EntityProcessor<BackgroundComponent>
    {
        public List<BackgroundComponent> Backgrounds { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundComponentProcessor"/> class.
        /// </summary>
        public BackgroundComponentProcessor()
        {
            Backgrounds = new List<BackgroundComponent>();
        }

        public override void Draw(RenderContext gameTime)
        {
            Backgrounds.Clear();
            foreach (var backgroundKeyPair in ComponentDatas)
            {
                var background = backgroundKeyPair.Key;
                if (background.Enabled)
                {
                    Backgrounds.Add(background);
                }
            }
        }
    }
}
