
using System;
using Stride.Audio;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Media;

namespace CSharpIntermediate.Code
{
    public class AudioDemo : SyncScript
    {
        public Sound UkeleleSound;

        private SoundInstance ukeleleInstance;
        private AudioEmitterComponent audioEmitterComponent;
        private AudioEmitterSoundController gunSoundEmitter;

        public override void Start()
        {
            // we neeed to create an instance of Sound object in order to play them
            ukeleleInstance = UkeleleSound.CreateInstance();

            audioEmitterComponent = Entity.Get<AudioEmitterComponent>();
            gunSoundEmitter = audioEmitterComponent["Gun"];
        }

        public override void Update()
        {
            // Play a sound
            DebugText.Print($"U to play the Ukelele once", new Int2(200, 40));
            if (Input.IsKeyPressed(Keys.U))
            {
                ukeleleInstance.Stop();
                ukeleleInstance.Play();
            }

            // Press left mouse button fire gun
            DebugText.Print($"Press left mouse button fire gun", new Int2(200, 60));
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                gunSoundEmitter.Play();
            }
        }
    }
}
