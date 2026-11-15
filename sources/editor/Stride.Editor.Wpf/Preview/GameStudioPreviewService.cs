// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Windows;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Controls;
using Stride.Editor.Engine;
using Stride.Games;

namespace Stride.Editor.Preview
{
    /// <summary>
    /// The preview service of a session in the WPF editor: the preview game renders in a hidden embedded form, hosted in
    /// a <see cref="GameEngineHost"/>.
    /// </summary>
    public sealed class GameStudioPreviewService : GameStudioPreviewServiceBase, IWpfPreviewBuilder
    {
        private readonly AutoResetEvent initializationSignal = new AutoResetEvent(false);
        private readonly GameEngineHost host;
        private EmbeddedGameForm gameForm;
        private IntPtr windowHandle;

        public GameStudioPreviewService(ISessionViewModel session)
            : base(session)
        {
            StartPreviewGame();

            // Wait for the window handle to be generated on the proper thread
            initializationSignal.WaitOne();
            host = new GameEngineHost(windowHandle);
        }

        public FrameworkElement GetStrideView()
        {
            return !IsDisposed ? host : null;
        }

        /// <inheritdoc />
        protected override GameContext CreateGameContext(PreviewGame game)
        {
            gameForm = new EmbeddedGameForm { TopLevel = false, Visible = false };
            windowHandle = gameForm.Handle;

            initializationSignal.Set();

            return new GameContextWinforms(gameForm) { InitializeDatabase = false };
        }

        /// <inheritdoc />
        protected override void DisposeView()
        {
            host.Dispose();
        }
    }
}
