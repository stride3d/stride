// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace SpriteStudioDemo
{
    /// <summary>
    /// The GUI script
    /// </summary>
    public class GuiScript : StartupScript
    {
		public SpriteFont Font { get; set; }
		
        public override void Start()
        {
            var font = Font;
            var textBlock = new TextBlock
            {
                Font = font,
                TextSize = 18,
                TextColor = Color.Gold,
                Text = "Shoot : Touch in a vertical section where the Agent resides\n" +
                       "Move : Touch in the screen on the corresponding side of the Agent",
            };
            textBlock.SetCanvasRelativePosition(new Vector3(0.008f, 0.9f, 0));

            Entity.Get<UIComponent>().Page = new UIPage { RootElement = new Canvas { Children = { textBlock } } };
        }
    }
}
