// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Engine;

namespace Stride.Audio.Tests.Engine
{
    /// <summary>
    /// Test the class <see cref="Game"/> augmented with the audio system.
    /// </summary>
    public class TestGame
    {
        /// <summary>
        /// Check that there is not problems during creation and destruction of the Game class.
        /// </summary>
        [Fact]
        public void TestCreationDestructionOfTheGame()
        {
            // Make sure this doesn't throw
            var game = new AudioTestGame();
            game.Dispose();
        }

        /// <summary>
        /// Check that we can access to the audio class and that it is not invalid.
        /// </summary>
        [Fact]
        public void TestAccessToAudio()
        {
            using (var game = new Game())
            {
                AudioSystem audioInterface = game.Audio;
                Assert.NotNull(audioInterface);
            }
        }

        /// <summary>
        /// The audio system is a declared game system (<see cref="Stride.Games.GameSystemAttribute"/>): the game creates it
        /// and registers its services in its constructor, before Initialize adds it to the game systems.
        /// </summary>
        [Fact]
        public void TestDeclaredSystemCreatedWithTheGame()
        {
            using (var game = new Game())
            {
                var audio = game.Services.GetService<AudioSystem>();
                Assert.NotNull(audio);
                Assert.Same(audio, game.Services.GetService<IAudioEngineProvider>());
                Assert.DoesNotContain(audio, game.GameSystems);
            }
        }
    }
}
