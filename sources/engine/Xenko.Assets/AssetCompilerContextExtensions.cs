// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core;
using Xenko.Graphics;

namespace Xenko.Assets
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
            if (context.OptionProperties.TryGetValue("XenkoGraphicsApi", out graphicsApi))
                return (GraphicsPlatform)Enum.Parse(typeof(GraphicsPlatform), graphicsApi);

            if (context.OptionProperties.TryGetValue("RuntimeIdentifier", out var runtimeIdentifier))
            {
                if (runtimeIdentifier.Contains("-d3d11"))
                    return GraphicsPlatform.Direct3D11;
                else if (runtimeIdentifier.Contains("-d3d12"))
                    return GraphicsPlatform.Direct3D12;
                // Note: testing opengles before opengl since one string contains another
                else if (runtimeIdentifier.Contains("-opengles"))
                    return GraphicsPlatform.OpenGLES;
                else if (runtimeIdentifier.Contains("-opengl"))
                    return GraphicsPlatform.OpenGL;
                else if (runtimeIdentifier.Contains("-vulkan"))
                    return GraphicsPlatform.Vulkan;
            }

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
