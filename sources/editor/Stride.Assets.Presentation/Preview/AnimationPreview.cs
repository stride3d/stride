// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;

using Stride.Core.Assets.Compiler;
using Stride.Animations;
using Stride.Assets.Models;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Assets.Presentation.ViewModel.Preview;
using Stride.Editor.Build;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Preview
{
    [AssetPreview(typeof(AnimationAsset), typeof(AnimationPreviewView))]
    public class AnimationPreview : PreviewFromEntity<AnimationAsset>
    {
        private readonly object animLock = new object();
        private PlayingAnimation playingAnim;
        private float timeFactor;

        private string compiledModelUrl;

        public Action<bool, float, float> UpdateViewModelTime { get; set; }

        public bool IsPlaying { get; private set; }

        protected override async Task Initialize()
        {
            await base.Initialize();

            Game.Script.AddTask(UpdateAnimationTime);
        }

        protected override void PrepareLoadedEntity()
        {
            lock (animLock)
                playingAnim = null;

            base.PrepareLoadedEntity();

            var animComponent = PreviewEntity.Entity.Get<AnimationComponent>();

            // Don't play, if the animation is invalid
            if (animComponent.Animations["preview"] != null)
            {
                animComponent.Play("preview");

                lock (animLock)
                {
                    playingAnim = animComponent.PlayingAnimations.Last();
                    playingAnim.RepeatMode = AnimationRepeatMode.LoopInfinite;
                }
            }
        }

        private async Task UpdateAnimationTime()
        {
            var noAnimTimeUpdated = false;
            while (IsRunning)
            {
                // Await two frames to reduce overhead
                await Game.Script.NextFrame();
                await Game.Script.NextFrame();
                if (UpdateViewModelTime != null)
                {
                    if (playingAnim != null)
                    {
                        UpdateViewModelTime(true, (float)playingAnim.CurrentTime.TotalSeconds, (float)playingAnim.Clip.Duration.TotalSeconds);
                        noAnimTimeUpdated = false;
                    }
                    else if (!noAnimTimeUpdated)
                    {
                        UpdateViewModelTime(false, 0.0f, 0.0f);
                        noAnimTimeUpdated = true;
                    }
                }
            }
        }

        protected override PreviewEntity CreatePreviewEntity()
        {
            if (compiledModelUrl == null)
                return null;

            // load the created material and the model from the data base
            var model = LoadAsset<Model>(compiledModelUrl);
            var anim = LoadAsset<AnimationClip>(AssetItem.Location);
            var animSrc = LoadAsset<AnimationClip>(AssetItem.Location + AnimationAssetCompiler.SrcClipSuffix);

            // create the entity, create and set the model component
            var entity = new Entity { Name = "Preview Entity of animation: " + AssetItem.Location };
            entity.Add(new ModelComponent { Model = model });
            // In case of additive animation, play the original source (we can't play the additive animation itself)
            entity.Add(new AnimationComponent { Animations = { { "preview", animSrc ?? anim } } });

            var previewEntity = new PreviewEntity(entity);
            previewEntity.Disposed += () => UnloadAsset(model);
            previewEntity.Disposed += () => UnloadAsset(anim);
            if (animSrc != null) previewEntity.Disposed += () => UnloadAsset(animSrc);

            return previewEntity;
        }

        protected override void UpdateBuildAssetResults(AnonymousAssetBuildUnit buildUnit, AssetCompilerResult compilationResult)
        {
            base.UpdateBuildAssetResults(buildUnit, compilationResult);

            // Save aside model url
            // TODO: we should technically store it in a temp var during Compile(), and set it only now (in case it changed during compile)
            compiledModelUrl = buildUnit.Succeeded ? AnimationPreviewViewModel.FindModelForPreview(AssetItem)?.Location : null;
        }

        public async void SetTimeScale(float value)
        {
            timeFactor = value;
            await IsInitialized();
            lock (animLock)
            {
                if (playingAnim != null)
                    playingAnim.TimeFactor = timeFactor;
            }
        }

        public void SetCurrentTime(float value)
        {
            lock (animLock)
            {
                if (playingAnim != null)
                {
                    value = (float)Math.Min(value, playingAnim.Clip.Duration.TotalSeconds - 0.001f);
                    // Remove one tick so the modulo in CurrentTime management won't mess up when we try to set the max value
                    playingAnim.CurrentTime = TimeSpan.FromSeconds(value);
                }
            }
        }

        public void Play()
        {
            IsPlaying = true;
            lock (animLock)
            {
                if (playingAnim != null)
                {
                    playingAnim.TimeFactor = timeFactor;
                    playingAnim.Enabled = IsPlaying;
                }
            }
        }

        public void Pause()
        {
            IsPlaying = false;
            lock (animLock)
            {
                if (playingAnim != null)
                    playingAnim.Enabled = IsPlaying;
            }
        }
    }
}
