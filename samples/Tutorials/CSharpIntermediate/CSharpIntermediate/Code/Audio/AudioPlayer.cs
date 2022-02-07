
using System;
using Stride.Audio;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Media;

namespace CSharpIntermediate.Code
{
    public class AudioPlayer : SyncScript
    {
        public Sound BackgroundMusic;

        private SoundInstance musicInstance;


        public override void Start()
        {
            musicInstance = BackgroundMusic.CreateInstance();
            musicInstance.IsLooping = true;
            musicInstance.PlayExclusive();
        }

        public override void Update()
        {
            // Play or pause
            DebugText.Print($"Space to play/pause. Currently: {musicInstance.PlayState}", new Int2(200, 20));
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

            // Volume or pause
            DebugText.Print($"Q/E to change volume: {musicInstance.Volume:0.0}", new Int2(200, 40));
            if (Input.IsKeyPressed(Keys.Q))
            {
                musicInstance.Volume = Math.Clamp(musicInstance.Volume + 0.1f, 0, 2);
            }
            if (Input.IsKeyPressed(Keys.E))
            {
                musicInstance.Volume = Math.Clamp(musicInstance.Volume - 0.1f, 0, 2);
            }

            // Panning audio 
            DebugText.Print($"A/D Pan: {musicInstance.Pan:0.0}", new Int2(200, 60));
            if (Input.IsKeyPressed(Keys.A))
            {
                musicInstance.Pan = Math.Clamp(musicInstance.Pan - 0.1f, -1, 1);
            }
            if (Input.IsKeyPressed(Keys.D))
            {
                musicInstance.Pan = Math.Clamp(musicInstance.Pan + 0.1f, -1, 1);
            }

            // Pitch audio
            DebugText.Print($"Z/C Pitch: {musicInstance.Pitch:0.0}", new Int2(200, 80));
            if (Input.IsKeyPressed(Keys.Z))
            {
                musicInstance.Pitch = Math.Clamp(musicInstance.Pitch - 0.1f, 0, 2);
            }
            if (Input.IsKeyPressed(Keys.C))
            {
                musicInstance.Pitch = Math.Clamp(musicInstance.Pitch + 0.1f, 0, 2);
            }
        }
    }
}
