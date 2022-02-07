
using Stride.Audio;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class AudioDemo : SyncScript
    {
        public Sound LevelCompletedSound;
        public Sound GunSound;
      
        private SoundInstance levelCompletedSoundInstance;
        private SoundInstance gunSoundInstance;


        public override void Start()
        {
            levelCompletedSoundInstance = LevelCompletedSound.CreateInstance();
            gunSoundInstance = GunSound.CreateInstance();

        }

        public override void Update()
        {
            //if (Input.IsKeyDown(Keys.Space) && levelCompletedSoundInstance.)
            //{
            //    // play the sound effect on each touch on the screen
            //    levelCompletedSoundInstance.Stop();
            //    levelCompletedSoundInstance.Play();
            //}
        }
    }
}
