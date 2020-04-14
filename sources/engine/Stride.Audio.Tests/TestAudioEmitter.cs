// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;

using Stride.Core.Mathematics;

namespace Stride.Audio.Tests
{
    /// <summary>
    /// Tests for <see cref="AudioEmitter"/>.
    /// </summary>
    public class TestAudioEmitter
    {
        private readonly AudioEmitter defaultEmitter = new AudioEmitter();

        /// <summary>
        /// Test the behaviour of the Position function.
        /// </summary>
        [Fact]
        public void TestPosition()
        {
            //////////////////////////////
            // 1. Check the default value
            Assert.True(Vector3.Zero == defaultEmitter.Position, "The AudioEmitter defaul location is not 0");

            ///////////////////////////////////////////
            // 2. Check for no crash and correct value
            defaultEmitter.Position = Vector3.One;
            Assert.True(Vector3.One == defaultEmitter.Position, "AudioEmitter.Position value is not what it is supposed to be.");
        }

        /// <summary>
        /// Test the behaviour of the Velocity function.
        /// </summary>
        [Fact]
        public void TestVelocity()
        {
            //////////////////////////////
            // 1. Check the default value
            Assert.True(Vector3.Zero == defaultEmitter.Velocity, "The AudioEmitter defaul Velocity is not 0");

            ///////////////////////////////////////////
            // 2. Check for no crash and correct value
            defaultEmitter.Velocity = Vector3.One;
            Assert.True(Vector3.One == defaultEmitter.Velocity, "AudioEmitter.Velocity value is not what it is supposed to be.");
        }

//        /// <summary>
//        /// Test the behaviour of the DopplerScale function.
//        /// </summary>
//        [Fact]
//        public void TestDopplerScale()
//        {
//            //////////////////////////////
//            // 1. Check the default value
//            Assert.True(1f, defaultEmitter.DopplerScale, "The AudioEmitter defaul DopplerScale is not 1");
//
//            ///////////////////////////////////////////
//            // 2. Check for no crash and correct value
//            defaultEmitter.DopplerScale = 5f;
//            Assert.Equal(5f, defaultEmitter.DopplerScale, "AudioEmitter.DopplerScale value is not what it is supposed to be.");
//
//            /////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that a negative value throws the 'ArgumentOutOfRangeException' exception
//            Assert.Throws<ArgumentOutOfRangeException>(() => defaultEmitter.DopplerScale = -1f, "AudioEmitter.DopplerScale did not throw 'ArgumentOutOfRangeException' when setting a negative value.");
//        }
//
//        /// <summary>
//        /// Test the behaviour of the DistanceScale function.
//        /// </summary>
//        [Fact]
//        public void TestDistanceScale()
//        {
//            //////////////////////////////
//            // 1. Check the default value
//            Assert.Equal(1f, defaultEmitter.DistanceScale, "The AudioEmitter defaul DistanceScale is not 1");
//
//            ///////////////////////////////////////////
//            // 2. Check for no crash and correct value
//            defaultEmitter.DistanceScale = 5f;
//            Assert.Equal(5f, defaultEmitter.DistanceScale, "AudioEmitter.DistanceScale value is not what it is supposed to be.");
//
//            /////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that a negative value throws the 'ArgumentOutOfRangeException' exception
//            Assert.Throws<ArgumentOutOfRangeException>(() => defaultEmitter.DistanceScale = -1f, "AudioEmitter.DistanceScale did not throw 'ArgumentOutOfRangeException' when setting a negative value.");
//        }
    }
}
