// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics.Regression;

namespace Stride.Audio.Tests.Engine
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

        public event Action<DefaultGame> LoadingContent;
        public event Action<DefaultGame> BeforeUpdating;
        public event Action<DefaultGame> AfterUpdating;
        public event Action<DefaultGame> BeforeDrawing;
        public event Action<DefaultGame> AfterDrawing;
    }
}
