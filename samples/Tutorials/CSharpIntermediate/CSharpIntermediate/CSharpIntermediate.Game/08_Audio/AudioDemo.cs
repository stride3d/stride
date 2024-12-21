// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Audio;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

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
            DebugText.Print($"U to play the Ukelele once", new Int2(200, 580));
            if (Input.IsKeyPressed(Keys.U))
            {
                ukuleleInstance.Stop();
                ukuleleInstance.Play();
            }

            // Press right mouse button for gun fire sound
            DebugText.Print($"Press right mouse button fire gun", new Int2(200, 640));
            if (Input.IsMouseButtonPressed(MouseButton.Right))
            {
                gunSoundEmitter.Play();
            }
        }
    }
}
