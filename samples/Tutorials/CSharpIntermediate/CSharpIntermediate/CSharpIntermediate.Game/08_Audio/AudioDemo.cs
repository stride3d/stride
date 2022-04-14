
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
        public Sound UkuleleSound;

        private SoundInstance ukuleleInstance;
        private AudioEmitterComponent audioEmitterComponent;
        private AudioEmitterSoundController gunSoundEmitter;

        public override void Start()
        {
            // We need to create an instance of Sound object in order to play them
            ukuleleInstance = UkuleleSound.CreateInstance();

            audioEmitterComponent = Entity.Get<AudioEmitterComponent>();
            gunSoundEmitter = audioEmitterComponent["Gun"];
        }

        public override void Update()
        {
            // Play a sound
            DebugText.Print($"U to play the Ukelele once", new Int2(200, 40));
            if (Input.IsKeyPressed(Keys.U))
            {
                ukuleleInstance.Stop();
                ukuleleInstance.Play();
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
