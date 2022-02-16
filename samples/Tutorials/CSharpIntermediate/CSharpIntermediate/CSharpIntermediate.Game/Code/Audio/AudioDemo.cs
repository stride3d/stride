
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
        public Sound BackgroundMusic;
        public Sound UkeleleMusic;

        private SoundInstance musicInstance;
        private SoundInstance ukeleleInstance;
        private AudioEmitterComponent audioEmitterComponent;
        private AudioEmitterSoundController gunSoundEmitter;

        public override void Start()
        {
            musicInstance = BackgroundMusic.CreateInstance();
            musicInstance.IsLooping = true;

            ukeleleInstance = UkeleleMusic.CreateInstance();

            audioEmitterComponent = Entity.Get<AudioEmitterComponent>();
            gunSoundEmitter = audioEmitterComponent["Gun"];
        }

        public override void Update()
        {
            // Play a sound
            DebugText.Print($"U to play the Ukelele once", new Int2(200, 20));
            if (Input.IsKeyPressed(Keys.U))
            {
                ukeleleInstance.Stop();
                ukeleleInstance.Play();
            }

            // Play or pause
            DebugText.Print($"Space to play/pause. Currently: {musicInstance.PlayState}", new Int2(200, 40));
            if (Input.IsKeyPressed(Keys.Space))
            {
                if(musicInstance.PlayState == PlayState.Playing)
                {
                    musicInstance.Pause();
                }
                else
                {
                    musicInstance.Play();
                }
            }

            // Volume 
            DebugText.Print($"Up/Down to change volume: {musicInstance.Volume:0.0}", new Int2(200, 60));
            if (Input.IsKeyPressed(Keys.Up))
            {
                musicInstance.Volume = Math.Clamp(musicInstance.Volume + 0.1f, 0, 2);
            }
            if (Input.IsKeyPressed(Keys.Down))
            {
                musicInstance.Volume = Math.Clamp(musicInstance.Volume - 0.1f, 0, 2);
            }

            // Panning
            DebugText.Print($"Left/Right to change panning: {musicInstance.Pan:0.0}", new Int2(200, 60));
            if (Input.IsKeyPressed(Keys.Left))
            {
                musicInstance.Pan = Math.Clamp(musicInstance.Pan + 0.1f, -1, 1);
            }
            if (Input.IsKeyPressed(Keys.Right))
            {
                musicInstance.Pan = Math.Clamp(musicInstance.Pan - 0.1f, -1, 1);
            }

            // Press left mouse button fire gun
            DebugText.Print($"Press left mouse button fire gun: {musicInstance.Volume:0.0}", new Int2(200, 80));
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                gunSoundEmitter.Play();
            }
        }
    }
}
