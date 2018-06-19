// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using NUnit.Framework;

using Xenko.Core.Mathematics;

namespace Xenko.Audio.Tests
{
    /// <summary>
    /// Tests for <see cref="AudioListener"/>.
    /// </summary>
    [TestFixture]
    public class TestAudioListener
    {
        private readonly AudioListener defaultListener = new AudioListener();

        /// <summary>
        /// Test the behaviour of the Position function.
        /// </summary>
        [Test]
        public void TestPosition()
        {
            //////////////////////////////
            // 1. Check the default value
            Assert.AreEqual(Vector3.Zero, defaultListener.Position, "The AudioListener defaul location is not 0");

            ///////////////////////////////////////////
            // 2. Check for no crash and correct value
            Assert.DoesNotThrow(() => defaultListener.Position = Vector3.One, "AudioListener.Position.Set crashed.");
            Assert.AreEqual(Vector3.One, defaultListener.Position, "AudioListener.Position value is not what it is supposed to be.");
        }

        /// <summary>
        /// Test the behaviour of the Velocity function.
        /// </summary>
        [Test]
        public void TestVelocity()
        {
            //////////////////////////////
            // 1. Check the default value
            Assert.AreEqual(Vector3.Zero, defaultListener.Velocity, "The AudioListener defaul Velocity is not 0");

            ///////////////////////////////////////////
            // 2. Check for no crash and correct value
            Assert.DoesNotThrow(() => defaultListener.Velocity = Vector3.One, "AudioListener.Velocity.Set crashed.");
            Assert.AreEqual(Vector3.One, defaultListener.Velocity, "AudioListener.Velocity value is not what it is supposed to be.");
        }

        /// <summary>
        /// Test the behaviour of the Forward function.
        /// </summary>
        [Test]
        public void TestForward()
        {
            //////////////////////////////
            // 1. Check the default value
            Assert.AreEqual(new Vector3(0,0,1), defaultListener.Forward, "The AudioListener default Forward is not (0,0,1)");

            ///////////////////////////////////////////
            // 2. Check for no crash and correct value
            var newVal = Vector3.Normalize(Vector3.One);
            Assert.DoesNotThrow(() => defaultListener.Forward = newVal, "AudioListener.Forward.Set crashed.");
            Assert.AreEqual(newVal, defaultListener.Forward, "AudioListener.Forward value is not what it is supposed to be.");

            //////////////////////////////////////////////////
            // 3. Check that Forward set normalize the vector
            defaultListener.Forward = new Vector3(2, 0, 0);
            Assert.AreEqual(new Vector3(1, 0, 0), defaultListener.Forward, "AudioListener.Forward.Set has not properly normalized the vector.");
            
            //////////////////////////////////////////////////////////////////////////////
            // 4. Check that Forward throws InvalidOperationException when vector is Zero
            Assert.Throws<InvalidOperationException>(() => defaultListener.Forward = Vector3.Zero, "AudioListener.Forward.Set has not thrown InvalidOperationException with a zero vector.");
        }

        /// <summary>
        /// Test the behaviour of the Up function.
        /// </summary>
        [Test]
        public void TestUp()
        {
            //////////////////////////////
            // 1. Check the default value
            Assert.AreEqual(new Vector3(0, 1, 0), defaultListener.Up, "The AudioListener default Up is not (0,0,1)");

            ///////////////////////////////////////////
            // 2. Check for no crash and correct value
            var newVal = Vector3.Normalize(Vector3.One);
            Assert.DoesNotThrow(() => defaultListener.Up = newVal, "AudioListener.Up.Set crashed.");
            Assert.AreEqual(newVal, defaultListener.Up, "AudioListener.Up value is not what it is supposed to be.");

            //////////////////////////////////////////////////
            // 3. Check that Up set normalize the vector
            defaultListener.Up = new Vector3(2, 0, 0);
            Assert.AreEqual(new Vector3(1, 0, 0), defaultListener.Up, "AudioListener.Up.Set has not properly normalized the vector.");

            //////////////////////////////////////////////////////////////////////////////
            // 4. Check that Up throws InvalidOperationException when vector is Zero
            Assert.Throws<InvalidOperationException>(() => defaultListener.Up = Vector3.Zero, "AudioListener.Up.Set has not thrown InvalidOperationException with a zero vector.");
        }
    }
}
