// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Audio;
using Stride.Engine;

namespace TopDownRPG.Gameplay
{
    /// <summary>
    /// The main script in charge of the sound.
    /// </summary>
    public class MusicScript : AsyncScript
    {
        public Sound SoundMusic { get; set; }

        private SoundInstance music;

        public override async Task Execute()
        {
            music = SoundMusic.CreateInstance();

            if (!IsLiveReloading)
            {
                // start ambient music
                music.IsLooping = true;
                music.Play();
            }

            while (Game.IsRunning)
            {
                // wait for next frame
                await Script.NextFrame();
            }
        }
    }
}
