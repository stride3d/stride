// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.SpriteStudio.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Serialization.Contents;
using Stride.Assets;
using Stride.Graphics;

namespace Stride.SpriteStudio.Offline
{
    [AssetCompiler(typeof(SpriteStudioAnimationAsset), typeof(AssetCompilationContext))]
    internal class SpriteStudioAnimationAssetCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SpriteStudioAnimationAsset)assetItem.Asset;
            var colorSpace = context.GetColorSpace();

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new SpriteStudioAnimationAssetCommand(targetUrlInStorage, asset, colorSpace, assetItem.Package));
        }

        /// <summary>
        /// Command used by the build engine to convert the asset
        /// </summary>
        private class SpriteStudioAnimationAssetCommand : AssetCommand<SpriteStudioAnimationAsset>
        {
            private ColorSpace colorSpace;

            public SpriteStudioAnimationAssetCommand(string url, SpriteStudioAnimationAsset asset, ColorSpace colorSpace, IAssetFinder assetFinder)
                : base(url, asset, assetFinder)
            {
                this.colorSpace = colorSpace;
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var nodes = new List<SpriteStudioNode>();
                string modelName;
                if (!SpriteStudioXmlImport.ParseModel(Parameters.Source, nodes, out modelName))
                {
                    return null;
                }

                var anims = new List<SpriteStudioAnim>();
                if (!SpriteStudioXmlImport.ParseAnimations(Parameters.Source, anims))
                {
                    return null;
                }

                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                var anim = anims.First(x => x.Name == Parameters.AnimationName);

                //Compile the animations
                var animation = new AnimationClip
                {
                    Duration = TimeSpan.FromSeconds((1.0 / anim.Fps) * anim.FrameCount),
                    RepeatMode = Parameters.RepeatMode
                };

                var nodeMapping = nodes.Select((x, i) => new { Name = x.Name, Index = i }).ToDictionary(x => x.Name, x => x.Index);

                foreach (var pair in anim.NodesData)
                {
                    int nodeIndex;
                    if (!nodeMapping.TryGetValue(pair.Key, out nodeIndex))
                        continue;

                    var data = pair.Value;
                    if (data.Data.Count == 0) continue;

                    var keyPrefix = $"[SpriteStudioComponent.Key].Nodes[{nodeIndex}]";

                    if (data.Data.TryGetValue("POSX", out List<Dictionary<string, string>> posX))
                    {
                        var posxCurve = new AnimationCurve<float>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.Position)}.{nameof(Vector2.X)}", posxCurve);
                        posxCurve.InterpolationType = posX.Any(x => x["curve"] != "linear") ? AnimationCurveInterpolationType.Cubic : AnimationCurveInterpolationType.Linear;

                        foreach (var nodeData in posX)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = float.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            posxCurve.KeyFrames.Add(new KeyFrameData<float>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("POSY", out List<Dictionary<string, string>> posY))
                    {
                        var posyCurve = new AnimationCurve<float>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.Position)}.{nameof(Vector2.Y)}", posyCurve);
                        posyCurve.InterpolationType = posY.Any(x => x["curve"] != "linear") ? AnimationCurveInterpolationType.Cubic : AnimationCurveInterpolationType.Linear;

                        foreach (var nodeData in posY)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = float.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            posyCurve.KeyFrames.Add(new KeyFrameData<float>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("ROTZ", out List<Dictionary<string, string>> rotZ))
                    {
                        var anglCurve = new AnimationCurve<float>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.RotationZ)}", anglCurve);
                        anglCurve.InterpolationType = rotZ.Any(x => x["curve"] != "linear") ? AnimationCurveInterpolationType.Cubic : AnimationCurveInterpolationType.Linear;

                        foreach (var nodeData in rotZ)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = MathUtil.DegreesToRadians(float.Parse(nodeData["value"], CultureInfo.InvariantCulture));
                            anglCurve.KeyFrames.Add(new KeyFrameData<float>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("PRIO", out List<Dictionary<string, string>> prio))
                    {
                        var prioCurve = new AnimationCurve<int>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.Priority)}", prioCurve);
                        prioCurve.InterpolationType = AnimationCurveInterpolationType.Constant;

                        foreach (var nodeData in prio)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = int.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            prioCurve.KeyFrames.Add(new KeyFrameData<int>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("SCLX", out List<Dictionary<string, string>> sclX))
                    {
                        var scaxCurve = new AnimationCurve<float>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.Scale)}.{nameof(Vector2.X)}", scaxCurve);
                        scaxCurve.InterpolationType = sclX.Any(x => x["curve"] != "linear") ? AnimationCurveInterpolationType.Cubic : AnimationCurveInterpolationType.Linear;

                        foreach (var nodeData in sclX)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = float.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            scaxCurve.KeyFrames.Add(new KeyFrameData<float>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("SCLY", out List<Dictionary<string, string>> sclY))
                    {
                        var scayCurve = new AnimationCurve<float>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.Scale)}.{nameof(Vector2.Y)}", scayCurve);
                        scayCurve.InterpolationType = sclY.Any(x => x["curve"] != "linear") ? AnimationCurveInterpolationType.Cubic : AnimationCurveInterpolationType.Linear;

                        foreach (var nodeData in sclY)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = float.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            scayCurve.KeyFrames.Add(new KeyFrameData<float>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("ALPH", out List<Dictionary<string, string>> alph))
                    {
                        var tranCurve = new AnimationCurve<float>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.Transparency)}", tranCurve);
                        tranCurve.InterpolationType = alph.Any(x => x["curve"] != "linear") ? AnimationCurveInterpolationType.Cubic : AnimationCurveInterpolationType.Linear;

                        foreach (var nodeData in alph)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = float.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            tranCurve.KeyFrames.Add(new KeyFrameData<float>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("HIDE", out List<Dictionary<string, string>> hide))
                    {
                        var hideCurve = new AnimationCurve<int>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.Hide)}", hideCurve);
                        hideCurve.InterpolationType = AnimationCurveInterpolationType.Constant;

                        foreach (var nodeData in hide)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = int.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            hideCurve.KeyFrames.Add(new KeyFrameData<int>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("FLPH", out List<Dictionary<string, string>> flph))
                    {
                        var flphCurve = new AnimationCurve<int>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.HFlipped)}", flphCurve);
                        flphCurve.InterpolationType = AnimationCurveInterpolationType.Constant;

                        foreach (var nodeData in flph)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = int.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            flphCurve.KeyFrames.Add(new KeyFrameData<int>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("FLPV", out List<Dictionary<string, string>> flpv))
                    {
                        var flpvCurve = new AnimationCurve<int>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.VFlipped)}", flpvCurve);
                        flpvCurve.InterpolationType = AnimationCurveInterpolationType.Constant;

                        foreach (var nodeData in flpv)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = int.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            flpvCurve.KeyFrames.Add(new KeyFrameData<int>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("CELL", out List<Dictionary<string, string>> cell))
                    {
                        var cellCurve = new AnimationCurve<int>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.SpriteId)}", cellCurve);
                        cellCurve.InterpolationType = AnimationCurveInterpolationType.Constant;

                        foreach (var nodeData in cell)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = int.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            cellCurve.KeyFrames.Add(new KeyFrameData<int>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("COLV", out List<Dictionary<string, string>> colv))
                    {
                        var colvCurve = new AnimationCurve<Vector4>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.BlendColor)}", colvCurve);
                        colvCurve.InterpolationType = AnimationCurveInterpolationType.Linear;

                        foreach (var nodeData in colv)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var color = new Color4(Color.FromBgra(int.Parse(nodeData["value"], CultureInfo.InvariantCulture)));
                            color = colorSpace == ColorSpace.Linear ? color.ToLinear() : color;
                            colvCurve.KeyFrames.Add(new KeyFrameData<Vector4>(time, color.ToVector4()));
                        }
                    }

                    if (data.Data.TryGetValue("COLB", out List<Dictionary<string, string>> colb))
                    {
                        var colbCurve = new AnimationCurve<int>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.BlendType)}", colbCurve);
                        colbCurve.InterpolationType = AnimationCurveInterpolationType.Constant;

                        foreach (var nodeData in colb)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = int.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            colbCurve.KeyFrames.Add(new KeyFrameData<int>(time, value));
                        }
                    }

                    if (data.Data.TryGetValue("COLF", out List<Dictionary<string, string>> colf))
                    {
                        var colfCurve = new AnimationCurve<float>();
                        animation.AddCurve($"{keyPrefix}.{nameof(SpriteStudioNodeState.BlendFactor)}", colfCurve);
                        colfCurve.InterpolationType = colf.Any(x => x["curve"] != "linear") ? AnimationCurveInterpolationType.Cubic : AnimationCurveInterpolationType.Linear;

                        foreach (var nodeData in colf)
                        {
                            var time = CompressedTimeSpan.FromSeconds((1.0 / anim.Fps) * int.Parse(nodeData["time"], CultureInfo.InvariantCulture));
                            var value = float.Parse(nodeData["value"], CultureInfo.InvariantCulture);
                            colfCurve.KeyFrames.Add(new KeyFrameData<float>(time, value));
                        }
                    }
                }

                animation.Optimize();

                assetManager.Save(Url, animation);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
