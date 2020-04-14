// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Controls;
using Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.SpriteEditor.Views
{
    /// <summary>
    /// Interaction logic for SpriteEditorView.xaml
    /// </summary>
    public partial class SpriteEditorView : IEditorView
    {
        static SpriteEditorView()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var resources = executingAssembly.GetManifestResourceNames();
            var stream = executingAssembly.GetManifestResourceStream(resources.First(x => x.EndsWith("ColorPicker.cur")));
            if (stream != null)
            {
                ColorPickerCursor = new Cursor(stream);
            }
            stream = executingAssembly.GetManifestResourceStream(resources.First(x => x.EndsWith("MagicWand.cur")));
            if (stream != null)
            {
                MagicWandCursor = new Cursor(stream);
            }
            FocusOnRegion = new RoutedCommand("FocusOnRegion", typeof(SpriteEditorView));
            ActivateMagicWand = new RoutedCommand("ActivateMagicWand", typeof(SpriteEditorView));
        }

        private readonly TaskCompletionSource<bool> editorInitializationNotifier = new TaskCompletionSource<bool>();

        public SpriteEditorView()
        {
            InitializeComponent();
            // Ensure we can give the focus to the editor
            Focusable = true;
        }

        public static RoutedCommand FocusOnRegion { get; }

        public static RoutedCommand ActivateMagicWand { get; }

        public static Cursor ColorPickerCursor { get; }

        public static Cursor MagicWandCursor { get; }

        public Task EditorInitialization => editorInitializationNotifier.Task;

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            // We give the focus to the editor so shortcuts will work
            if (!IsKeyboardFocusWithin && !(e.OriginalSource is GameEngineHost))
            {
                Keyboard.Focus(this);
            }
        }

        public async Task<IAssetEditorViewModel> InitializeEditor(AssetViewModel asset)
        {
            var spriteSheet = (SpriteSheetViewModel)asset;
            var editor = new SpriteSheetEditorViewModel(spriteSheet);
            var result = await editor.Initialize();
            editorInitializationNotifier.SetResult(result);
            return editor;
        }
    }
}
