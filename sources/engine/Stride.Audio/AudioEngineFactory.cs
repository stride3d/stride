// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Audio
{
    public static class AudioEngineFactory
    {
        /// <summary>
        /// Based on compilation setting, returns the proper instance of sounds.
        /// </summary>
        /// <returns>A platform specific instance of <see cref="AudioEngine"/></returns>
        public static AudioEngine NewAudioEngine(AudioDevice device = null, AudioLayer.DeviceFlags deviceFlags = AudioLayer.DeviceFlags.None)
        {
            AudioEngine engine = null;
#if STRIDE_PLATFORM_IOS
            engine = new AudioEngineIos();
#else
            engine = new AudioEngine(device);
#endif
            engine.InitializeAudioEngine(deviceFlags);
            return engine;
        }
    }
}
