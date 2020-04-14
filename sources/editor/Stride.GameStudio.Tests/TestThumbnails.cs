// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Diagnostics;
using Xenko.Assets.SpriteFont;
using Xenko.Rendering;
using Xenko.Rendering.Materials;
using Xenko.Graphics;

namespace Xenko.GameStudio.Tests
{
    public class TestThumbnails
    {
        private readonly PackageSession projectSession;
        private readonly IThumbnailService thumbnailService;
        
        private TaskCompletionSource<int> tcs;

        private int assetCount;

        static TestThumbnails()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(SpriteFontAsset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(MaterialKeys).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(Model).Module.ModuleHandle);
        }

        public TestThumbnails()
        {
            // load assembly to register the assets extensions
            RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);
            var assembly = Assembly.Load("Xenko.Assets.Presentation");
            foreach (var module in assembly.Modules)
            {
                RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
            }

            // load the project
            var projectSessionResult = PackageSession.Load("..\\..\\sources\\tools\\Xenko.Previewer.Tests\\Assets\\Assets.xkpkg");
            projectSession = projectSessionResult.Session;
            //projectSession = PackageSession.Load(@"C:\Dev\sengokurun\SengokuRun\SengokuRun\GameAssets\SengokuRun.xkpkg");

            // find an entity in the project
            //previewAsset = projectSession.FindAsset("mc00_entity");
            // create the asset previewer and subscribe to the build progress events
            // TODO: desactivated
            //thumbnailService = new GameStudioPreviewService(projectSession, null, "..\\..\\sources\\tools\\Xenko.Previewer.Tests\\obj", null).ThumbnailService;
            //thumbnailService.ThumbnailCompleted += PreviewerOnThumbnailBuilt; 
        }

        public void Run()
        {

            tcs = new TaskCompletionSource<int>();

            var allAssets = projectSession.Packages.SelectMany(x => x.Assets).ToList();
            assetCount = allAssets.Count;
            thumbnailService.AddThumbnailAssetItems(allAssets, QueuePosition.First);

            tcs.Task.Wait();
        }

        //private int thumbnailBuilt;
        //private void PreviewerOnThumbnailBuilt(object sender, ThumbnailCompletedArgs e)
        //{
        //    var image = Image.Load(e.Data.ThumbnailStream);
        //    var asset = projectSession.FindAsset(e.AssetId);
        //    using (var stream = File.OpenWrite(asset + ".png"))
        //        image.Save(stream, ImageFileType.Png);
        //
        //    ++thumbnailBuilt;
        //
        //    if (thumbnailBuilt == assetCount)
        //        tcs.SetResult(0);
        //}
    }
}
