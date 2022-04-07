
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Stride.Audio;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Media;

namespace CSharpIntermediate.Code
{
    public class LoadMusic : AsyncScript
    {
        public Sound BackgroundMusic;

        private SoundInstance musicInstance;

        public override async Task Execute()
        {
            musicInstance = BackgroundMusic.CreateInstance();

            // Wait till the music is done loading
            await musicInstance.ReadyToPlay();

            while (Game.IsRunning)
            {
                // Play or pause
                DebugText.Print($"Space to play/pause. Currently: {musicInstance.PlayState}", new Int2(800, 40));
                if (Input.IsKeyPressed(Keys.Space))
                {
                    if (musicInstance.PlayState == PlayState.Playing)
                    {
                        musicInstance.Pause();
                    }
                    else
                    {
                        musicInstance.Play();
                    }
                }

                // Volume 
                DebugText.Print($"Up/Down to change volume: {musicInstance.Volume:0.0}", new Int2(800, 60));
                if (Input.IsKeyPressed(Keys.Up))
                {
                    musicInstance.Volume = Math.Clamp(musicInstance.Volume + 0.1f, 0, 2);
                }
                if (Input.IsKeyPressed(Keys.Down))
                {
                    musicInstance.Volume = Math.Clamp(musicInstance.Volume - 0.1f, 0, 2);
                }

                // Panning
                DebugText.Print($"Left/Right to change panning: {musicInstance.Pan:0.0}", new Int2(800, 80));
                if (Input.IsKeyPressed(Keys.Left))
                {
                    musicInstance.Pan = Math.Clamp(musicInstance.Pan - 0.1f, -1, 1);
                }
                if (Input.IsKeyPressed(Keys.Right))
                {
                    musicInstance.Pan = Math.Clamp(musicInstance.Pan + 0.1f, -1, 1);
                }

                // Wait for next frame
                await Script.NextFrame();
            }
        }
    }
}
