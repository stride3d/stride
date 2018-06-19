// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics.Regression;

namespace Xenko.Audio.Tests.Engine
{
    internal class GameClassForTests : GameTestBase
    {
        protected bool ContentLoaded;

        protected override void Update(GameTime gameTime)
        {
            LoadContent().Wait();

            BeforeUpdating?.Invoke(this);

            base.Update(gameTime);

            AfterUpdating?.Invoke(this);
        }

        protected override void Draw(GameTime gameTime)
        {
            LoadContent().Wait();

            BeforeDrawing?.Invoke(this);

            base.Draw(gameTime);

            AfterDrawing?.Invoke(this);
        }

        protected override async Task LoadContent()
        {
            if (ContentLoaded)
                return;

            await base.LoadContent();

            LoadingContent?.Invoke(this);

            ContentLoaded = true;
        }

        public event Action<Game> LoadingContent;
        public event Action<Game> BeforeUpdating;
        public event Action<Game> AfterUpdating;
        public event Action<Game> BeforeDrawing;
        public event Action<Game> AfterDrawing;
    }
}
