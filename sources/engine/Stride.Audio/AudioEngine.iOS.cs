// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS

using AVFoundation;
using Stride.Native;

namespace Stride.Audio
{
    public class AudioEngineIos : AudioEngine
    {
        internal override void InitializeAudioEngine(AudioLayer.DeviceFlags flags)
        {
            ActivateAudioSession();
            base.InitializeAudioEngine(flags);
        }

        private void ActivateAudioSession()
        {
            const double preferedAudioLatency = 0.005;

            // start the AudioSession
            var audioSession = AVAudioSession.SharedInstance();

            AVAudioSession.Notifications.ObserveInterruption((sender, args) =>
            {
                if (args.InterruptionType == AVAudioSessionInterruptionType.Began)
                {
                    AudioLayer.ListenerDisable(DefaultListener.Listener);
                }
                else
                {
                    AudioLayer.ListenerEnable(DefaultListener.Listener);
                }
            });

            // set the audio category so that: playback/recording is possible, playback is mixed with background music, music is stopped when screen is locked
            var error = audioSession.SetCategory(AVAudioSessionCategory.SoloAmbient);
            if (error != null)
            {
                Logger.Warning($"Failed to set the audio category to 'Ambient'. [Error info: {error.UserInfo}]");
                State = AudioEngineState.Invalidated;
                return;
            }

            // Reduce the buffer size for better latency response..
            audioSession.SetPreferredIOBufferDuration(preferedAudioLatency, out error);
            if (error != null)
                Logger.Warning($"Failed to set the audio IO buffer duration to '{preferedAudioLatency}'. [Error info: {error.UserInfo}]");

            // set the preferred sampling rate of the application
            if (AudioSampleRate != 0)
            {
                audioSession.SetPreferredSampleRate(AudioSampleRate, out error);
                Logger.Warning($"Failed to set the audio session preferred sampling rate to '{AudioSampleRate}'. [Error info: {error.UserInfo}]");
            }

            // activate the sound for the application
            error = audioSession.SetActive(true);
            if (error != null)
            {
                Logger.Warning($"Failed to activate the audio session. [Error info: {error.UserInfo}]");
                State = AudioEngineState.Invalidated;
            }
        }
    }
}
#endif
