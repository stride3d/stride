// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core;
using Stride.Graphics;

namespace Stride.Assets
{
    public static class AssetCompilerContextExtensions
    {
        private static readonly PropertyKey<GameSettingsAsset> GameSettingsAssetKey = new PropertyKey<GameSettingsAsset>("GameSettingsAsset", typeof(AssetCompilerContextExtensions));

        public static GameSettingsAsset GetGameSettingsAsset(this AssetCompilerContext context)
        {
            return context.Properties.Get(GameSettingsAssetKey);
        }

        public static ColorSpace GetColorSpace(this AssetCompilerContext context)
        {
            var settings = context.GetGameSettingsAsset().GetOrCreate<RenderingSettings>(context.Platform);
            return settings.ColorSpace;
        }

        public static void SetGameSettingsAsset(this AssetCompilerContext context, GameSettingsAsset gameSettingsAsset)
        {
            context.Properties.Set(GameSettingsAssetKey, gameSettingsAsset);
        }

        public static GraphicsPlatform GetGraphicsPlatform(this AssetCompilerContext context, Package package)
        {
            // If we have a command line override, use it first
            string graphicsApi;
            if (context.OptionProperties.TryGetValue("StrideGraphicsApi", out graphicsApi))
                return (GraphicsPlatform)Enum.Parse(typeof(GraphicsPlatform), graphicsApi);

            // Ohterwise, use default as fallback
            return context.Platform.GetDefaultGraphicsPlatform();
        }

        public static GraphicsPlatform GetDefaultGraphicsPlatform(this PlatformType platformType)
        {
            switch (platformType)
            {
                case PlatformType.Windows:
                case PlatformType.UWP:
                    return GraphicsPlatform.Direct3D11;
                case PlatformType.Android:
                case PlatformType.iOS:
                    return GraphicsPlatform.OpenGLES;
                case PlatformType.Linux:
                    return GraphicsPlatform.OpenGL;
                case PlatformType.macOS:
                    return GraphicsPlatform.Vulkan;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // TODO: Move that as extension method?
        public static CompilationMode GetCompilationMode(this AssetCompilerContext context)
        {
            var compilationMode = CompilationMode.Debug;
            switch (context.BuildConfiguration)
            {
                case "Debug":
                    compilationMode = CompilationMode.Debug;
                    break;
                case "Release":
                    compilationMode = CompilationMode.Release;
                    break;
                case "AppStore":
                    compilationMode = CompilationMode.AppStore;
                    break;
                case "Testing":
                    compilationMode = CompilationMode.Testing;
                    break;
            }
            return compilationMode;
        }
    }
}
