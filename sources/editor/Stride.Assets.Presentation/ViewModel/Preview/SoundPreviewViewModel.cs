// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.Preview;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(SoundPreview))]
    public class SoundPreviewViewModel : AssetPreviewViewModel
    {
        private SoundPreview soundPreview;
        private TimeSpan currentTime;
        private TimeSpan duration;
        private double masterVolume = 1.0;
        private bool isAudioValid;
        private volatile bool updatingFromGame;

        public SoundPreviewViewModel(SessionViewModel session)
            : base(session)
        {
            PlayCommand = new AnonymousCommand(ServiceProvider, Play);
            PauseCommand = new AnonymousCommand(ServiceProvider, Pause);
        }
        
        public bool IsAudioValid { get { return isAudioValid; } set { SetValue(ref isAudioValid, value); } }
        
        public double MasterVolume { get { return masterVolume; } set { SetValue(ref masterVolume, value); soundPreview.SetMasterVolume(value); } }

        public double CurrentValue { get { return CurrentTime.TotalSeconds; } set { CurrentTime = TimeSpan.FromSeconds(value); } }

        public TimeSpan CurrentTime { get { return currentTime; } private set { SetValue(ref currentTime, value, nameof(CurrentTime), nameof(CurrentValue)); if (!updatingFromGame) soundPreview.SetCurrentTime(value); } }

        public TimeSpan Duration { get { return duration; } private set { SetValue(ref duration, value); } }

        public ICommandBase PlayCommand { get; }

        public ICommandBase PauseCommand { get; }

        public override void AttachPreview(IAssetPreview preview)
        {
            soundPreview = (SoundPreview)preview;
            soundPreview.ProvideDispatcher(Dispatcher);
            PlayCommand.IsEnabled = !soundPreview.IsPlaying;
            PauseCommand.IsEnabled = soundPreview.IsPlaying;
            soundPreview.UpdateViewModelTime += UpdateViewModelTime;
        }

        private void UpdateViewModelTime(bool hasAudio, bool isPlaying, TimeSpan current, TimeSpan soundDuration)
        {
            if (updatingFromGame)
                return;

            updatingFromGame = true;
            Dispatcher.InvokeAsync(() =>
            {
                PlayCommand.IsEnabled = !isPlaying;
                PauseCommand.IsEnabled = isPlaying;
                CurrentTime = current;
                IsAudioValid = hasAudio;
                Duration = soundDuration;
                updatingFromGame = false;
            });
        }

        private void Play()
        {
            soundPreview.Play();
            PlayCommand.IsEnabled = false;
            PauseCommand.IsEnabled = true;
        }

        private void Pause()
        {
            soundPreview.Pause();
            PlayCommand.IsEnabled = true;
            PauseCommand.IsEnabled = false;
        }
    }
}
