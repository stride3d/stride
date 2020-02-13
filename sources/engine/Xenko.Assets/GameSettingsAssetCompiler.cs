// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Data;
using Xenko.Engine;
using Xenko.Engine.Design;
using Xenko.Graphics;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets
{
    [AssetCompiler(typeof(GameSettingsAsset), typeof(AssetCompilationContext))]
    public class GameSettingsAssetCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (GameSettingsAsset)assetItem.Asset;
            // TODO: We should ignore game settings stored in dependencies
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new GameSettingsCompileCommand(targetUrlInStorage, assetItem.Package, context.Package, context.Platform, context.GetCompilationMode(), asset));
        }

        public override IEnumerable<Type> GetRuntimeTypes(AssetItem assetItem)
        {
            yield return typeof(Scene);
            yield return typeof(GraphicsCompositor);
            yield return typeof(Texture);
        }

        private class GameSettingsCompileCommand : AssetCommand<GameSettingsAsset>
        {
            private readonly PlatformType platform;
            private readonly CompilationMode compilationMode;
            private readonly Package package;
            private readonly Package entryPackage;
            public GameSettingsCompileCommand(string url, Package package, Package entryPackage, PlatformType platform, CompilationMode compilationMode, GameSettingsAsset asset)
                : base(url, asset, package)
            {
                this.package = package;
                this.entryPackage = entryPackage ?? package;
                this.platform = platform;
                this.compilationMode = compilationMode;
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);

                // Hash used parameters from package
                writer.Write(package.Meta.Name);
                writer.Write(entryPackage.UserSettings.GetValue(GameUserSettings.Effect.EffectCompilation));
                writer.Write(entryPackage.UserSettings.GetValue(GameUserSettings.Effect.RecordUsedEffects));
                writer.Write(compilationMode);

                // Hash platform
                writer.Write(platform);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var result = new GameSettings
                {
                    PackageName = package.Meta.Name,
                    DefaultSceneUrl = Parameters.DefaultScene != null ? AttachedReferenceManager.GetUrl(Parameters.DefaultScene) : null,
                    DefaultGraphicsCompositorUrl = Parameters.GraphicsCompositor != null ? AttachedReferenceManager.GetUrl(Parameters.GraphicsCompositor) : null,
                    SplashScreenUrl = Parameters.SplashScreenTexture != null && (compilationMode == CompilationMode.Release || compilationMode == CompilationMode.AppStore) ? AttachedReferenceManager.GetUrl(Parameters.SplashScreenTexture) : null,
                    SplashScreenColor = Parameters.SplashScreenColor,
                    DoubleViewSplashScreen = Parameters.DoubleViewSplashScreen,
                    EffectCompilation = entryPackage.UserSettings.GetValue(GameUserSettings.Effect.EffectCompilation),
                    RecordUsedEffects = entryPackage.UserSettings.GetValue(GameUserSettings.Effect.RecordUsedEffects),
                    Configurations = new PlatformConfigurations(),
                    CompilationMode = compilationMode
                };

                //start from the default platform and go down overriding

                foreach (var configuration in Parameters.Defaults.Where(x => !x.OfflineOnly))
                {
                    result.Configurations.Configurations.Add(new ConfigurationOverride
                    {
                        Platforms = ConfigPlatforms.None,
                        SpecificFilter = -1,
                        Configuration = configuration
                    });
                }

                foreach (var configurationOverride in Parameters.Overrides.Where(x => x.Configuration != null && !x.Configuration.OfflineOnly))
                {
                    result.Configurations.Configurations.Add(configurationOverride);
                }

                result.Configurations.PlatformFilters = Parameters.PlatformFilters;

                //make sure we modify platform specific files to set the wanted orientation
                if (package.Container is SolutionProject solutionProject)
                    SetPlatformOrientation(solutionProject, Parameters.GetOrCreate<RenderingSettings>().DisplayOrientation);

                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                assetManager.Save(Url, result);

                return Task.FromResult(ResultStatus.Successful);
            }
        }

        public static void SetPlatformOrientation(SolutionProject project, RequiredDisplayOrientation orientation)
        {
            switch (project.Platform)
            {
                case PlatformType.Android:
                    {
                        if (project.FullPath == null) return;

                        var activityFileName = project.Name + "Activity.cs";
                        var activityFile = UPath.Combine(project.FullPath.GetFullDirectory(), new UFile(activityFileName));
                        if (!File.Exists(activityFile)) return;

                        var activitySource = File.ReadAllText(activityFile);

                        string orientationString;
                        switch (orientation)
                        {
                            case RequiredDisplayOrientation.Default:
                                orientationString = "Android.Content.PM.ScreenOrientation.Landscape";
                                break;
                            case RequiredDisplayOrientation.LandscapeLeft:
                                orientationString = "Android.Content.PM.ScreenOrientation.Landscape";
                                break;
                            case RequiredDisplayOrientation.LandscapeRight:
                                orientationString = "Android.Content.PM.ScreenOrientation.ReverseLandscape";
                                break;
                            case RequiredDisplayOrientation.Portrait:
                                orientationString = "Android.Content.PM.ScreenOrientation.Portrait";
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        activitySource = Regex.Replace(activitySource, @"(\[Activity(?:.*[\n,\r]*)+?[\n,\r,\s]*ScreenOrientation\s*=\s*)([\w,\d,\.]+)(\s*,)", $"$1{orientationString}$3");

                        File.WriteAllText(activityFile, activitySource);
                    }
                    break;
                case PlatformType.iOS:
                    {
                        var exeProjectLocation = project.FullPath;
                        if (exeProjectLocation == null) return;

                        var plistFile = UPath.Combine(exeProjectLocation.GetFullDirectory(), new UFile("Info.plist"));
                        if (!File.Exists(plistFile)) return;

                        var xmlDoc = XDocument.Load(plistFile);
                        var orientationKey = xmlDoc.Descendants("key").FirstOrDefault(x => x.Value == "UISupportedInterfaceOrientations");
                        var orientationElement = ((XElement)orientationKey?.NextNode)?.Descendants("string").FirstOrDefault();
                        if (orientationElement != null)
                        {
                            switch (orientation)
                            {
                                case RequiredDisplayOrientation.Default:
                                    orientationElement.Value = "UIInterfaceOrientationLandscapeRight";
                                    break;
                                case RequiredDisplayOrientation.LandscapeLeft:
                                    orientationElement.Value = "UIInterfaceOrientationLandscapeLeft";
                                    break;
                                case RequiredDisplayOrientation.LandscapeRight:
                                    orientationElement.Value = "UIInterfaceOrientationLandscapeRight";
                                    break;
                                case RequiredDisplayOrientation.Portrait:
                                    orientationElement.Value = "UIInterfaceOrientationPortrait";
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        xmlDoc.Save(plistFile);
                    }
                    break;
            }
        }
    }
}
