// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.IO;
using Stride.Assets.SpriteFont;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
using Stride.Graphics;

namespace Stride.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(SpriteFontAsset))]
    public class SpriteFontViewModel : AssetViewModel<SpriteFontAsset>
    {
        /// <summary>
        /// The name of the category for font-related properties. Must match the category name in the <see cref="DisplayAttribute"/> of these properties.
        /// </summary>
        public const string FontCategory = "Font";
        /// <summary>
        /// The name of the category for character-related properties. Must match the category name in the <see cref="DisplayAttribute"/> of these properties.
        /// </summary>
        public const string CharactersCategory = "Characters";

        public SpriteFontViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
            GeneratePrecompiledFontCommand = new AnonymousTaskCommand(ServiceProvider, GeneratePrecompiledFont);
            assetCommands.Add(new MenuCommandInfo(ServiceProvider, GeneratePrecompiledFontCommand) { DisplayName = "Generate precompiled font", Tooltip = "Generate precompiled font" });
        }

        public ICommandBase GeneratePrecompiledFontCommand { get; }

        private async Task GeneratePrecompiledFont()
        {
            var font = (SpriteFontAsset)AssetItem.Asset;
            var dialogService = ServiceProvider.Get<IDialogService>();
            // Dynamic font cannot be precompiled
            if (font.FontType is RuntimeRasterizedSpriteFontType)
            {
                // Note: Markdown (**, _) are used to format the text.
                await dialogService.MessageBox(Tr._p("Message", "**Only static fonts can be precompiled.**\r\n\r\nClear the _Is Dynamic_ property on this font and try again."), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Compute unique name
            var precompiledName = NamingHelper.ComputeNewName($"{AssetItem.Location.GetFileNameWithoutExtension()} (Precompiled)", Directory.Assets, x => x.Name);

            // Ask location for generated texture
            var folderDialog = dialogService.CreateFolderOpenModalDialog();
            folderDialog.InitialDirectory = (Session.CurrentProject?.Package?.RootDirectory ?? Session.SolutionPath.GetFullDirectory()).ToWindowsPath() + "\\Resources";
            var dialogResult = await folderDialog.ShowModal();
            if (dialogResult != DialogResult.Ok)
                return;

            bool srgb;
            var gameSettings = Session.CurrentProject?.Package.GetGameSettingsAsset();
            if (gameSettings == null)
            {
                var buttons = DialogHelper.CreateButtons(new[] { ColorSpace.Linear.ToString(), ColorSpace.Gamma.ToString(), Tr._p("Button", "Cancel") }, 1, 3);
                var result = await dialogService.MessageBox(Tr._p("Message", "Which color space do you want to use?"), buttons, MessageBoxImage.Question);
                // Close without clicking a button or Cancel
                if (result == 0 || result == 3)
                    return;
                srgb = result == 2;
            }
            else
            {
                srgb = gameSettings.GetOrCreate<RenderingSettings>().ColorSpace == ColorSpace.Linear;
            }

            var precompiledFontAsset = (font.FontType is SignedDistanceFieldSpriteFontType) ?
                font.GeneratePrecompiledSDFSpriteFont(AssetItem, UFile.Combine(folderDialog.Directory, precompiledName)) : 
                font.GeneratePrecompiledSpriteFont(AssetItem, UFile.Combine(folderDialog.Directory, precompiledName), srgb);

            // NOTE: following code could be factorized with AssetFactoryViewModel
            var defaultLocation = UFile.Combine(Directory.Path, precompiledName);
            var assetItem = new AssetItem(defaultLocation, precompiledFontAsset);
            AssetViewModel assetViewModel;
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                // FIXME: do we need to delete the generated file upon undo?
                assetViewModel = Directory.Package.CreateAsset(Directory, assetItem, true, null);
                UndoRedoService.SetName(transaction, $"Create Asset '{precompiledName}'");
            }

            Session.CheckConsistency();
            if (assetViewModel != null)
            {
                Session.ActiveAssetView.SelectAssetCommand.Execute(assetViewModel);
            }
        }
    }
}
