// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Animations;
using Stride.Assets.Materials;

namespace Stride.Assets.Models
{
    [AssetCompiler(typeof(AnimationAsset), typeof(AssetCompilationContext))]
    public class AnimationAssetCompiler : AssetCompilerBase
    {
        public const string RefClipSuffix = "_reference_clip";
        public const string SrcClipSuffix = "_source_clip";

        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(SkeletonAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (AnimationAsset)assetItem.Asset;
            var assetAbsolutePath = assetItem.FullPath;
            // Get absolute path of asset source on disk
            var assetDirectory = assetAbsolutePath.GetParent();
            var assetSource = GetAbsolutePath(assetItem, asset.Source);
            var extension = assetSource.GetFileExtension();
            var buildStep = new AssetBuildStep(assetItem);

            // Find skeleton asset, if any
            AssetItem skeleton = null;
            if (asset.Skeleton != null)
                skeleton = assetItem.Package.FindAssetFromProxyObject(asset.Skeleton);

            var sourceBuildCommand = ImportModelCommand.Create(extension);
            if (sourceBuildCommand == null)
            {
                result.Error($"No importer found for model extension '{extension}. The model '{assetSource}' can't be imported.");
                return;
            }

            sourceBuildCommand.Mode = ImportModelCommand.ExportMode.Animation;
            sourceBuildCommand.SourcePath = assetSource;
            sourceBuildCommand.Location = targetUrlInStorage;
            sourceBuildCommand.AnimationRepeatMode = asset.RepeatMode;
            sourceBuildCommand.AnimationRootMotion = asset.RootMotion;
            sourceBuildCommand.ImportCustomAttributes = asset.ImportCustomAttributes;
            if (asset.ClipDuration.Enabled)
            {
                sourceBuildCommand.StartFrame = asset.ClipDuration.StartAnimationTime;
                sourceBuildCommand.EndFrame = asset.ClipDuration.EndAnimationTime;
            }
            else
            {
                sourceBuildCommand.StartFrame = TimeSpan.Zero;
                sourceBuildCommand.EndFrame = AnimationAsset.LongestTimeSpan;
            }
            sourceBuildCommand.ScaleImport = asset.ScaleImport;
            sourceBuildCommand.PivotPosition = asset.PivotPosition;

            if (skeleton != null)
            {
                sourceBuildCommand.SkeletonUrl = skeleton.Location;
                // Note: skeleton override values
                sourceBuildCommand.ScaleImport = ((SkeletonAsset)skeleton.Asset).ScaleImport;
                sourceBuildCommand.PivotPosition = ((SkeletonAsset)skeleton.Asset).PivotPosition;
            }

            if (asset.Type.Type == AnimationAssetTypeEnum.AnimationClip)
            {
                // Import the main animation
                buildStep.Add(sourceBuildCommand);
            }
            else if (asset.Type.Type == AnimationAssetTypeEnum.DifferenceClip)
            {
                var diffAnimationAsset = ((DifferenceAnimationAssetType)asset.Type);
                var referenceClip = diffAnimationAsset.BaseSource;
                var rebaseMode = diffAnimationAsset.Mode;

                var baseUrlInStorage = targetUrlInStorage + RefClipSuffix;
                var sourceUrlInStorage = targetUrlInStorage + SrcClipSuffix;

                var baseAssetSource = UPath.Combine(assetDirectory, referenceClip);
                var baseExtension = baseAssetSource.GetFileExtension();

                sourceBuildCommand.Location = sourceUrlInStorage;

                var baseBuildCommand = ImportModelCommand.Create(extension);
                if (baseBuildCommand == null)
                {
                    result.Error($"No importer found for model extension '{baseExtension}. The model '{baseAssetSource}' can't be imported.");
                    return;
                }

                baseBuildCommand.FailOnEmptyAnimation = false;
                baseBuildCommand.Mode = ImportModelCommand.ExportMode.Animation;
                baseBuildCommand.SourcePath = baseAssetSource;
                baseBuildCommand.Location = baseUrlInStorage;
                baseBuildCommand.AnimationRepeatMode = asset.RepeatMode;
                baseBuildCommand.AnimationRootMotion = asset.RootMotion;

                if (diffAnimationAsset.ClipDuration.Enabled)
                {
                    baseBuildCommand.StartFrame = diffAnimationAsset.ClipDuration.StartAnimationTimeBox;
                    baseBuildCommand.EndFrame = diffAnimationAsset.ClipDuration.EndAnimationTimeBox;
                }
                else
                {
                    baseBuildCommand.StartFrame = TimeSpan.Zero;
                    baseBuildCommand.EndFrame = AnimationAsset.LongestTimeSpan;
                }

                baseBuildCommand.ScaleImport = asset.ScaleImport;
                baseBuildCommand.PivotPosition = asset.PivotPosition;

                if (skeleton != null)
                {
                    baseBuildCommand.SkeletonUrl = skeleton.Location;
                    // Note: skeleton override values
                    baseBuildCommand.ScaleImport = ((SkeletonAsset)skeleton.Asset).ScaleImport;
                    baseBuildCommand.PivotPosition = ((SkeletonAsset)skeleton.Asset).PivotPosition;
                }

                // Import base and main animation
                var sourceStep = new CommandBuildStep(sourceBuildCommand);
                buildStep.Add(sourceStep);
                var baseStep = new CommandBuildStep(baseBuildCommand);
                buildStep.Add(baseStep);
              
                IEnumerable<ObjectUrl> InputFilesGetter()
                {
                    yield return new ObjectUrl(UrlType.File, GetAbsolutePath(assetItem, diffAnimationAsset.BaseSource));
                }

                var diffCommand = new AdditiveAnimationCommand(targetUrlInStorage, new AdditiveAnimationParameters(baseUrlInStorage, sourceUrlInStorage, rebaseMode), assetItem.Package)
                {
                    InputFilesGetter = InputFilesGetter
                };

                var diffStep = new CommandBuildStep(diffCommand);

                BuildStep.LinkBuildSteps(sourceStep, diffStep);
                BuildStep.LinkBuildSteps(baseStep, diffStep);

                // Generate the diff of those two animations
                buildStep.Add(diffStep);
            }
            else
            {
                throw new NotImplementedException("This type of animation asset is not supported yet!");
            }

            result.BuildSteps = buildStep;
        }

        internal class AdditiveAnimationCommand : AssetCommand<AdditiveAnimationParameters>
        {
            public AdditiveAnimationCommand(string url, AdditiveAnimationParameters parameters, IAssetFinder assetFinder) :
                base(url, parameters, assetFinder)
            {
                Version = 3;
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                // Load source and base animations
                var baseAnimation = assetManager.Load<AnimationClip>(Parameters.BaseUrl);
                var sourceAnimation = assetManager.Load<AnimationClip>(Parameters.SourceUrl);

                // Generate diff animation
                var animation = (baseAnimation == null) ? sourceAnimation : SubtractAnimations(baseAnimation, sourceAnimation);

                // Optimize animation
                animation.Optimize();

                // Save diff animation
                assetManager.Save(Url, animation);

                return Task.FromResult(ResultStatus.Successful);
            }

            private AnimationClip SubtractAnimations(AnimationClip baseAnimation, AnimationClip sourceAnimation)
            {
                if (baseAnimation == null) throw new ArgumentNullException("baseAnimation");
                if (sourceAnimation == null) throw new ArgumentNullException("sourceAnimation");

                var animationBlender = new AnimationBlender();

                var baseEvaluator = animationBlender.CreateEvaluator(baseAnimation);
                var sourceEvaluator = animationBlender.CreateEvaluator(sourceAnimation);

                // Create a result animation with same channels
                var resultAnimation = new AnimationClip();
                foreach (var channel in sourceAnimation.Channels)
                {
                    // Create new instance of curve
                    var newCurve = (AnimationCurve)Activator.CreateInstance(typeof(AnimationCurve<>).MakeGenericType(channel.Value.ElementType));

                    // Quaternion curve are linear, others are cubic
                    if (newCurve.ElementType != typeof(Quaternion))
                        newCurve.InterpolationType = AnimationCurveInterpolationType.Cubic;

                    resultAnimation.AddCurve(channel.Key, newCurve);
                }

                var resultEvaluator = animationBlender.CreateEvaluator(resultAnimation);

                var animationOperations = new FastList<AnimationOperation>();

                // Perform animation blending for each frame and upload results in a new animation
                // Note that it does a simple per-frame sampling, so animation discontinuities will be lost.
                // TODO: Framerate is hardcoded at 30 FPS.
                var frameTime = TimeSpan.FromSeconds(1.0f / 30.0f);
                for (var time = TimeSpan.Zero; time < sourceAnimation.Duration + frameTime; time += frameTime)
                {
                    // Last frame, round it to end of animation
                    if (time > sourceAnimation.Duration)
                        time = sourceAnimation.Duration;

                    TimeSpan baseTime;
                    switch (Parameters.Mode)
                    {
                        case AdditiveAnimationBaseMode.FirstFrame:
                            baseTime = TimeSpan.Zero;
                            break;
                        case AdditiveAnimationBaseMode.Animation:
                            baseTime = TimeSpan.FromTicks(time.Ticks % baseAnimation.Duration.Ticks);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // Generates result = source - base
                    animationOperations.Clear();
                    animationOperations.Add(AnimationOperation.NewPush(sourceEvaluator, time));
                    animationOperations.Add(AnimationOperation.NewPush(baseEvaluator, baseTime));
                    animationOperations.Add(AnimationOperation.NewBlend(CoreAnimationOperation.Subtract, 1.0f));
                    animationOperations.Add(AnimationOperation.NewPop(resultEvaluator, time));
                    
                    // Compute
                    AnimationClipResult animationClipResult = null;
                    animationBlender.Compute(animationOperations, ref animationClipResult);
                }

                resultAnimation.Duration = sourceAnimation.Duration;
                resultAnimation.RepeatMode = sourceAnimation.RepeatMode;

                return resultAnimation;
            }
        }

        [DataContract]
        public class AdditiveAnimationParameters
        {
            public string BaseUrl;
            public string SourceUrl;
            public AdditiveAnimationBaseMode Mode;
            public int BaseStartFrame;

            public AdditiveAnimationParameters()
            {
            }

            public AdditiveAnimationParameters(string baseUrl, string sourceUrl, AdditiveAnimationBaseMode mode)
            {
                BaseUrl = baseUrl;
                SourceUrl = sourceUrl;
                Mode = mode;
            }
        }
    }
}
