// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Threading.Tasks;
using Stride.Core.IO;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Core.Presentation.Services;
using Stride.Assets.Media;
using Stride.Audio;
using Stride.Editor.Preview;
using Stride.Media;

namespace Stride.Assets.Presentation.Preview
{
    [AssetPreview(typeof(SoundAsset), typeof(SoundPreviewView))]
    public class SoundPreview : BuildAssetPreview<SoundAsset>
    {
        private IDispatcherService dispatcher;

        private Sound sound;
        private SoundInstance instance;

        private TimeSpan startTime = TimeSpan.Zero;

        public Action<bool, bool, TimeSpan, TimeSpan> UpdateViewModelTime { get; set; }

        public bool IsPlaying { get; private set; }

        private volatile bool loaded;

        public void ProvideDispatcher(IDispatcherService dispatcherService)
        {
            dispatcher = dispatcherService;
        }

        protected bool VerifyAssetConsistency()
        {
            // Get absolute path of asset source on disk
            var assetDirectory = (string)AssetItem.Location.GetParent();
            var assetSource = UPath.Combine(assetDirectory, Asset.Source ?? "");

            // Check that the source file exist on the disk.
            return File.Exists(assetSource);
        }

        protected override async Task Initialize()
        {
            await base.Initialize();
            await Task.Run(() => UpdatePlaybackTime());
        }

        protected override async Task<bool> PrepareContent()
        {
            loaded = false;

            startTime = TimeSpan.Zero;

            if (sound != null)
            {
                instance.Stop();
                instance.Dispose();
                UnloadAsset(sound);
            }

            await base.PrepareContent();

            sound = LoadAsset<Sound>(AssetItem.Location);
            if (sound == null)
                return false;
            instance = sound.CreateInstance(null, true);
            instance.SetRange(new PlayRange(TimeSpan.Zero, TimeSpan.Zero));

            loaded = true;
            return true;
        }

        protected override void UnloadContent()
        {
            if (!loaded)
                return;

            UpdateViewModelTime(false, false, TimeSpan.Zero, TimeSpan.Zero);
            instance.Stop();
            instance.Dispose();
            instance = null;
            UnloadAsset(sound);

            loaded = false;
        }

        private async void UpdatePlaybackTime()
        {
            try
            {
                while (IsRunning)
                {
                    await Task.Delay(50);
                    if (UpdateViewModelTime != null)
                    {
                        await dispatcher.InvokeAsync(() =>
                        {
                            if (loaded)
                            {
                                IsPlaying = instance.PlayState == PlayState.Playing;
                                var position = instance.Position;
                                UpdateViewModelTime(true, IsPlaying, position + startTime, sound.TotalLength);
                            }
                            else
                            {
                                UpdateViewModelTime(false, false, TimeSpan.Zero, TimeSpan.Zero);
                            }
                        });
                    }
                }
            }
            catch (TaskCanceledException)
            {
                //Cool!
            }
        }

        public void SetCurrentTime(TimeSpan value)
        {
            var wasPlaying = instance?.PlayState == PlayState.Playing;

            instance?.Stop();
            startTime = value;
            instance?.SetRange(new PlayRange(value, sound.TotalLength));

            if (wasPlaying)
                instance?.Play();
        }

        public void Play()
        {
            instance?.Play();
        }

        public void Pause()
        {
            instance?.Pause();
        }

        public void SetMasterVolume(double value)
        {
            if (instance != null)
            {
                instance.Volume = (float)value;
            }
        }
    }
}
