// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace GravitySensor
{
    public class GuiScript : AsyncScript
    {
        public SpriteFont Font;

        public override async Task Execute()
        {
            if (Input.Gravity != null) // do not display any message when orientation sensor is available
                return;

            if (IsLiveReloading)
                return;

            var textBlock = new TextBlock
            {
                Text = "Use arrows to play with gravity!",
                Font = Font,
                TextColor = Color.White,
                TextSize = 40
            };
            textBlock.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 0));
            textBlock.SetCanvasRelativePosition(new Vector3(0.5f, 0.75f, 0f));
            Entity.Get<UIComponent>().Page = new UIPage { RootElement = new Canvas { Children = { textBlock } } };

            while (Game.IsRunning)
            {
                await Script.NextFrame();

                if (!Input.IsKeyPressed(Keys.Left) && !Input.IsKeyPressed(Keys.Right) && !Input.IsKeyPressed(Keys.Up) &&
                    !Input.IsKeyPressed(Keys.Down))
                    continue;

                Entity.Get<UIComponent>().Page = null;
                return;
            }
        }
    }
}
