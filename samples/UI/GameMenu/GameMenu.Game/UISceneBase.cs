// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace GameMenu
{
    public abstract class UISceneBase : SyncScript
    {
        protected Game UIGame;

        protected bool IsRunning;

        protected bool SceneCreated;

        public override void Start()
        {
            IsRunning = true;

            UIGame = (Game)Services.GetServiceAs<IGame>();

            AdjustVirtualResolution(this, EventArgs.Empty);
            Game.Window.ClientSizeChanged += AdjustVirtualResolution;

            CreateScene();
        }

        public override void Update()
        {
            UpdateScene();
        }

        protected virtual void UpdateScene()
        {
        }

        public override void Cancel()
        {
            Game.Window.ClientSizeChanged -= AdjustVirtualResolution;

            IsRunning = false;
            SceneCreated = false;
        }

        private void AdjustVirtualResolution(object sender, EventArgs e)
        {
            var backBufferSize = new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);
            Entity.Get<UIComponent>().Resolution = new Vector3(backBufferSize, 1000);
        }

        protected void CreateScene()
        {
            if (!SceneCreated)
                LoadScene();

            SceneCreated = true;
        }

        protected abstract void LoadScene();
    }
}
