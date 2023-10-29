// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<AnimationPreview>]
public sealed class AnimationPreviewViewModel : AssetPreviewViewModel<AnimationPreview>, IAnimatedPreviewViewModel
{
    private AnimationPreview animationPreview;
    private float timeScale = 1.0f;
    private float currentTime;
    private float duration;
    private bool isValid;
    private volatile bool updatingFromGame;

    public AnimationPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
        PlayCommand = new AnonymousCommand(ServiceProvider, Play);
        PauseCommand = new AnonymousCommand(ServiceProvider, Pause);
    }

    public float TimeScale { get { return timeScale; } set { SetValue(ref timeScale, value, () => animationPreview.SetTimeScale(value)); } }

    public float CurrentTime { get { return currentTime; } set { SetValue(ref currentTime, value); if (!updatingFromGame) animationPreview.SetCurrentTime(value); } }

    public float Duration { get { return duration; } private set { SetValue(ref duration, value); } }

    public bool IsValid { get { return isValid; } private set { SetValue(ref isValid, value); } }

    public ICommandBase PlayCommand { get; }

    public ICommandBase PauseCommand { get; }

    public bool IsPlaying => animationPreview?.IsPlaying ?? false;

    protected override void OnAttachPreview(AnimationPreview preview)
    {
        animationPreview = preview;
        animationPreview.SetTimeScale(timeScale);
        currentTime = 0.0f;
        animationPreview.UpdateViewModelTime = UpdateViewModelTime;

        // Automatically play the animation
        Play();
    }

    public static AssetItem? FindModelForPreview(AssetItem assetItem)
    {
        var animationAsset = (AnimationAsset)assetItem.Asset;
        var previewModelAsset = animationAsset?.PreviewModel != null ? assetItem.Package.FindAssetFromProxyObject(animationAsset.PreviewModel) : null;
        return previewModelAsset?.Asset is ModelAsset { Skeleton: not null } ? previewModelAsset : null;
    }

    private void UpdateViewModelTime(bool isTimeValid, float current, float animDuration)
    {
        if (updatingFromGame)
            return;

        updatingFromGame = true;
        Dispatcher.LowPriorityInvokeAsync(() =>
        {
            IsValid = isTimeValid;
            CurrentTime = current;
            Duration = animDuration;
            updatingFromGame = false;
        });
    }

    private void Play()
    {
        animationPreview.Play();
        PlayCommand.IsEnabled = false;
        PauseCommand.IsEnabled = true;
    }

    private void Pause()
    {
        animationPreview.Pause();
        PlayCommand.IsEnabled = true;
        PauseCommand.IsEnabled = false;
    }
}
