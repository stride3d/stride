// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace UIElementLink
{
    public class SplashScript : UISceneBase
    {
        public SpriteSheet SplashScreenImages;

        public UrlReference<Scene> NextScene;

        private Button followedButton;

        private Vector2 centerPoint;

        private void LoadNextScene()
        {
            if (NextScene?.IsEmpty ?? true)
                return;

            SceneSystem.SceneInstance.RootScene = Content.Load(NextScene);
            Cancel();
        }

        protected override void LoadScene()
        {
            Game.Window.AllowUserResizing = false;

            var backBufferSize = new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);
            centerPoint = new Vector2(backBufferSize.X / 2, backBufferSize.Y / 2);

            var longButton = (SpriteFromTexture) SplashScreenImages["button_long"];
            var longSize = new Vector3(SplashScreenImages["button_long"].SizeInPixels.X,
                SplashScreenImages["button_long"].SizeInPixels.Y, 0);

            // This button will be followed
            followedButton = new Button
            {
                PressedImage = longButton,
                NotPressedImage = longButton,
                MouseOverImage =  longButton,

                Size = longSize,

                // This element will be followed, because we have specified the same name in the FollowingEntity's UI Element Link
                Name = "ElementName",
            };

            // Load the next scene when the user clicks the button
            followedButton.Click += delegate { LoadNextScene(); };

            // Corner buttons
            var boxButton = (SpriteFromTexture)SplashScreenImages["button_box"];
            var boxSize = new Vector3(SplashScreenImages["button_box"].SizeInPixels.X,
                SplashScreenImages["button_box"].SizeInPixels.Y, 0);

            var cornerTL = new Button { PressedImage = boxButton, NotPressedImage = boxButton, MouseOverImage = boxButton, Size = boxSize };
            cornerTL.SetCanvasAbsolutePosition(new Vector3(0, 0, 0));

            var cornerTR = new Button { PressedImage = boxButton, NotPressedImage = boxButton, MouseOverImage = boxButton, Size = boxSize };
            cornerTR.SetCanvasAbsolutePosition(new Vector3(backBufferSize.X - boxSize.X, 0, 0));

            var cornerBL = new Button { PressedImage = boxButton, NotPressedImage = boxButton, MouseOverImage = boxButton, Size = boxSize };
            cornerBL.SetCanvasAbsolutePosition(new Vector3(0, backBufferSize.Y - boxSize.Y, 0));

            var cornerBR = new Button { PressedImage = boxButton, NotPressedImage = boxButton, MouseOverImage = boxButton, Size = boxSize };
            cornerBR.SetCanvasAbsolutePosition(new Vector3(backBufferSize.X - boxSize.X, backBufferSize.Y - boxSize.Y, 0));

            var rootElement = new Canvas() { Children = { followedButton, cornerTL, cornerTR, cornerBL, cornerBR },
                MaximumWidth = backBufferSize.X, MaximumHeight = backBufferSize.Y };

            Entity.Get<UIComponent>().Page = new UIPage { RootElement = rootElement };
        }

        protected override void UpdateScene()
        {
            // Move the followed button around
            var distance = (float) Math.Sin(Game.UpdateTime.Total.TotalSeconds * 0.2f) * centerPoint.X * 0.75f;
            followedButton.SetCanvasAbsolutePosition(new Vector3(centerPoint.X + distance, centerPoint.Y, 0));
        }
    }
}
