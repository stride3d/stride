using Stride.Audio;
using Stride.Engine;
using Stride.Input;
using Stride.Media;

namespace CSharpIntermediate.Code
{
    public class SpatialAudioDemo : SyncScript
    {
        public Sound GunSound;
        private SoundInstance gunSoundInstance;

        public override void Start()
        { 
            gunSoundInstance = GunSound.CreateInstance();
        }

        public override void Update()
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left) && gunSoundInstance.PlayState != PlayState.Playing)
            {
                gunSoundInstance.Play();
            }
        }
    }
}
