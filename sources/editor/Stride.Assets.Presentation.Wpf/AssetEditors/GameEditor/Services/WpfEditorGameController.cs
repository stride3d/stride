// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Forms;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Controls;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;
using Point = System.Windows.Point;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>Non-generic internal hook so external assemblies (test harness) can reach the
    /// underlying <see cref="EditorServiceGame"/> without depending on the closed generic type.</summary>
    internal interface IEditorGameAccess
    {
        EditorServiceGame EditorGame { get; }
    }

    /// <summary>
    /// The controller of the game of a WPF editor: the shared <see cref="EditorGameController{TEditorGame}"/>, with the
    /// game in a WinForms form (<see cref="WpfEditorGameViewport"/>), and what only this hosting gives: the cursor, the
    /// mouse position, drag and drop out of the game, and the recovery service.
    /// </summary>
    /// <typeparam name="TEditorGame">The type of the game.</typeparam>
    public abstract partial class WpfEditorGameController<TEditorGame> : EditorGameController<TEditorGame>, IEditorGameAccess
        where TEditorGame : EditorServiceGame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WpfEditorGameController{TEditorGame}"/> class.
        /// </summary>
        /// <param name="editor">The editor associated with this instance.</param>
        /// <param name="gameFactory">The factory to create the editor game.</param>
        protected WpfEditorGameController([NotNull] GameEditorViewModel editor, [NotNull] EditorGameFactory<TEditorGame> gameFactory)
            : base(editor, WpfEditorGameViewport.Service, gameFactory)
        {
        }

        EditorServiceGame IEditorGameAccess.EditorGame => Game;

        /// <summary>
        /// Gets the WPF element that shows the game (UI thread, once the game started).
        /// </summary>
        public GameEngineHost EditorHost => WpfViewport.Host;

        /// <summary>
        /// The distance, in screen pixels, a pointer moves while pressed before it counts as a drag.
        /// </summary>
        public Vector2 MinimumDragDistance => new((float)System.Windows.SystemParameters.MinimumHorizontalDragDistance, (float)System.Windows.SystemParameters.MinimumVerticalDragDistance);

        /// <summary>
        /// Gets the editor of the asset.
        /// </summary>
        protected new GameEditorViewModel Editor => (GameEditorViewModel)base.Editor;

        /// <summary>
        /// The game form hosting the scene view.
        /// </summary>
        protected EmbeddedGameForm GameForm => WpfViewport.Form;

        protected EditorGameRecoveryService RecoveryService { get; private set; }

        private WpfEditorGameViewport WpfViewport => (WpfEditorGameViewport)Viewport;

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed();
            if (!IsDestroying)
            {
                UnregisterFromDragDropEvents();
                WpfViewport.DisposeHost();
            }
            base.Destroy();
        }

        public Vector3 GetMousePositionInScene(bool lastRightClick)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return Vector3.Zero;
            var mousePosition = lastRightClick ? WpfViewport.LastClickPosition : Control.MousePosition;
            var host = EditorHost;
            var localPosition = host.PointFromScreen(new Point(mousePosition.X, mousePosition.Y));
            var relativePos = new Vector2((float)(localPosition.X / host.ActualWidth), (float)(localPosition.Y / host.ActualHeight));
            return Game.GetPositionInScene(relativePos);
        }

        public void ChangeCursor(EditorGameCursor cursor)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return;
            GameForm.Cursor = cursor switch
            {
                EditorGameCursor.No => Cursors.No,
                EditorGameCursor.SizeAll => Cursors.SizeAll,
                EditorGameCursor.SizeNS => Cursors.SizeNS,
                EditorGameCursor.SizeWE => Cursors.SizeWE,
                EditorGameCursor.SizeNWSE => Cursors.SizeNWSE,
                EditorGameCursor.SizeNESW => Cursors.SizeNESW,
                _ => null,
            };
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Also listens to the drag and drop events of the form (game thread, before the game runs), and turns the recovery
        /// service on.
        /// </remarks>
        protected override void InitializeServices(EditorGameServiceRegistry services)
        {
            RegisterToDragDropEvents();
            base.InitializeServices(services);

            RecoveryService = services.Get<EditorGameRecoveryService>();
            if (RecoveryService != null)
                RecoveryService.IsActive = true;
        }

        partial void RegisterToDragDropEvents();

        partial void UnregisterFromDragDropEvents();
    }
}
