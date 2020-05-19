// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Stride.Games;
using Stride.Input;

namespace Stride.Audio.Tests
{
    public class BasicTest : AudioTestGame
    {
        public BasicTest()
        {
        }


        private int count;
        private Sound effectA;
        private Sound musicA;
        private Sound effect48kHz;
        private Sound effect11kHz;
        private Sound effect22kHz;
        private Sound effect11kHzStereo;
        private Sound effect22kHzStereo;

        protected override Task LoadContent()
        {
            effect48kHz = Content.Load<Sound>("Effect48000Hz");
            effect11kHz = Content.Load<Sound>("Effect11025Hz");
            effect22kHz = Content.Load<Sound>("Effect22050Hz");
            effect11kHzStereo = Content.Load<Sound>("Effect11025HzStereo");
            effect22kHzStereo = Content.Load<Sound>("Effect22050HzStereo");

            effectA = Content.Load<Sound>("EffectToneA");
            musicA = Content.Load<Sound>("MusicToneA");

            return Task.FromResult(true);
        }
        
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.PointerEvents.Count > 0)
            {
                if (Input.PointerEvents.Any(x => x.EventType == PointerEventType.Released))
                {
                    if (count % 5 == 0)
                        effect48kHz.CreateInstance(Audio.AudioEngine.DefaultListener).Play();
                    else if (count % 5 == 1)
                        effect11kHz.CreateInstance(Audio.AudioEngine.DefaultListener).Play();
                    else if (count % 5 == 2)
                        effect22kHz.CreateInstance(Audio.AudioEngine.DefaultListener).Play();
                    else if (count % 5 == 3)
                        effect11kHzStereo.CreateInstance(Audio.AudioEngine.DefaultListener).Play();
                    else if (count % 5 == 4)
                        effect22kHzStereo.CreateInstance(Audio.AudioEngine.DefaultListener).Play();

                    count++;
                }
            }
        }

        [Fact]
        public void RunBasicGame()
        {
            RunGameTest(new BasicTest());
        }
    }
}
