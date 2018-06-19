// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;

using Xenko.Engine;

namespace Xenko.Audio.Tests.Engine
{
    /// <summary>
    /// Test the class <see cref="Game"/> augmented with the audio system.
    /// </summary>
    [TestFixture]
    class TestGame
    {
        /// <summary>
        /// Check that there is not problems during creation and destruction of the Game class.
        /// </summary>
        [Test]
        public void TestCreationDestructionOfTheGame()
        {
            AudioTestGame game = null;
            Assert.DoesNotThrow(() => game = new AudioTestGame(), "Creation of the Game failed");
            Assert.DoesNotThrow(()=> game.Dispose(), "Disposal of the Game failed");
        }

        /// <summary>
        /// Check that we can access to the audio class and that it is not invalid.
        /// </summary>
        [Test]
        public void TestAccessToAudio()
        {
            using (var game = new Game())
            {
                AudioSystem audioInterface = null;
                Assert.DoesNotThrow(()=>audioInterface = game.Audio, "Failed to get the audio interface");
                Assert.IsNotNull(audioInterface, "The audio interface supplied is null");
            }
        }
    }
}
