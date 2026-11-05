// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Forms;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Controls;
using Stride.Editor.Engine;
using Stride.Games;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// The viewport of the WPF editors: the game renders into a WinForms form of its own (an <see cref="EmbeddedGameForm"/>,
    /// created on the game thread), which a <see cref="GameEngineHost"/> shows in the WPF view.
    /// </summary>
    /// <remarks>
    /// The game has a window, so this viewport is not its <see cref="EmbeddedGame.Host"/>: the members of
    /// <see cref="IEmbeddedGameHost"/> do nothing.
    /// </remarks>
    internal sealed class WpfEditorGameViewport : IEditorGameViewport
    {
        /// <summary>
        /// Creates the viewports of the WPF editors.
        /// </summary>
        public static readonly IEditorGameViewportService Service = new ViewportService();

        private IntPtr windowHandle;

        /// <summary>
        /// Gets the form the game renders into, or <c>null</c> before the game thread created it.
        /// </summary>
        public EmbeddedGameForm Form { get; private set; }

        /// <summary>
        /// Gets the screen position of the last mouse button press on the form.
        /// </summary>
        public System.Drawing.Point LastClickPosition { get; private set; }

        /// <summary>
        /// Gets the WPF element that shows the form (UI thread, once the game started), created on first access.
        /// </summary>
        public GameEngineHost Host
        {
            get
            {
                if (Form == null)
                    throw new InvalidOperationException("The game has not started.");
                return Form.Host ??= new GameEngineHost(windowHandle);
            }
        }

        /// <summary>
        /// Gets whether the editor closed: the WPF element that showed the form is disposed.
        /// </summary>
        public bool IsReleased { get; private set; }

        /// <inheritdoc/>
        public object View => Host;

        /// <inheritdoc/>
        public GameContext Attach(EmbeddedGame game)
        {
            // Create the form from the game thread
            Form = new EmbeddedGameForm
            {
                TopLevel = false,
                Visible = false,
            };
            windowHandle = Form.Handle;
            Form.MouseDown += (sender, e) => LastClickPosition = Control.MousePosition;
            return new GameContextWinforms(Form) { InitializeDatabase = false };
        }

        /// <summary>
        /// Disposes the WPF element that shows the form, if it was created (UI thread, when the controller is destroyed).
        /// </summary>
        public void ReleaseView()
        {
            IsReleased = true;
            Form?.Host?.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // The game disposes its own window, after the loop's last present
        }

        void IEmbeddedGameHost.Initialize(EmbeddedGame game) { }

        void IEmbeddedGameHost.BeginDraw(EmbeddedGame game) { }

        void IEmbeddedGameHost.EndDraw(EmbeddedGame game, bool present) { }

        void IEmbeddedGameHost.AfterEndDraw(EmbeddedGame game, bool present) { }

        private sealed class ViewportService : IEditorGameViewportService
        {
            public IEditorGameViewport CreateViewport(Logger logger) => new WpfEditorGameViewport();
        }
    }
}
