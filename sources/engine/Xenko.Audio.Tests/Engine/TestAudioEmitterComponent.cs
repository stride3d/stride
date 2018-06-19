//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// This file is distributed under GPL v3. See LICENSE.md for details.
//
//using System;
//
//using NUnit.Framework;
//
//using Xenko.Core.IO;
//using Xenko.Core.Serialization.Assets;
//using Xenko.Engine;
//
//namespace Xenko.Audio.Tests.Engine
//{
//    /// <summary>
//    /// Test the <see cref="AudioEmitterComponent"/>. Some of the test depends on internal implementation and can failed if implementation has been modified.
//    /// </summary>
//    [TestFixture]
//    public class TestAudioEmitterComponent
//    {
//        /// <summary>
//        /// Test Component default values and states.
//        /// </summary>
//        [Test]
//        public void TestInitialization()
//        {
//            var testInst = new AudioEmitterComponent();
//            Assert.AreEqual(testInst.DistanceScale, 1, "Default value of the distance scale is not correct");
//            Assert.AreEqual(testInst.DopplerScale, 1, "Default value of the doppler scale is not correct");
//            Assert.IsFalse(testInst.ShouldBeProcessed, "Default value of ShouldBeProcessed si not correct");
//            Assert.IsTrue(testInst.SoundEffectToController.Keys.Count ==0, "Controller list is not empty");
//        }
//
//        [Test]
//        public void TestDistanceDopplerScale()
//        {
//            var testInst = new AudioEmitterComponent();
//            Assert.Throws<ArgumentOutOfRangeException>(() => testInst.DistanceScale = -0.1f, "DistanceScale did not throw ArgumentOutOfRangeException");
//            Assert.Throws<ArgumentOutOfRangeException>(() => testInst.DopplerScale = -0.1f, "DopplerScale did not throw ArgumentOutOfRangeException");
//        }
//
//        /// <summary>
//        /// Test the internal behaviour of the component when attaching and detaching sound to the component.
//        /// </summary>
//        [Test]
//        public void TestAttachDetachSounds()
//        {
//            var testInst = new AudioEmitterComponent();
//            using (var game = new Game())
//            {
//                Game.InitializeAssetDatabase();
//                using (var audioStream1 = ContentManager.FileProvider.OpenStream("EffectToneA", VirtualFileMode.Open, VirtualFileAccess.Read))
//                using (var audioStream2 = ContentManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
//                using (var audioStream3 = ContentManager.FileProvider.OpenStream("EffectStereo", VirtualFileMode.Open, VirtualFileAccess.Read))
//                {
//                    var sound1 = SoundEffect.Load(game.Audio.AudioEngine, audioStream1);
//                    var sound2 = SoundEffect.Load(game.Audio.AudioEngine, audioStream2);
//                    var sound3 = SoundEffect.Load(game.Audio.AudioEngine, audioStream3);
//
//                    // Attach two soundEffect and check that their controller are correctly created.
//
//                    AudioEmitterSoundController soundController1 = null;
//                    AudioEmitterSoundController soundController2 = null;
//                    Assert.DoesNotThrow(() => testInst.AttachSoundEffect(sound1), "Adding a first soundEffect failed");
//                    Assert.DoesNotThrow(() => soundController1 = testInst.SoundEffectToController[sound1], "There are no sound controller for sound1.");
//                    Assert.IsNotNull(soundController1, "Sound controller for sound1 is null");
//
//                    Assert.DoesNotThrow(() => testInst.AttachSoundEffect(sound2), "Adding a second soundEffect failed");
//                    Assert.DoesNotThrow(() => soundController2 = testInst.SoundEffectToController[sound2], "There are no sound controller for sound1.");
//                    Assert.IsNotNull(soundController2, "Sound controller for sound2 is null");
//
//                    // Remove the two soundEffect and check that their controller are correctly erased.
//
//                    Assert.DoesNotThrow(() => testInst.DetachSoundEffect(sound2), "Removing a first soundEffect failed");
//                    Assert.IsFalse(testInst.SoundEffectToController.ContainsKey(sound2), "The controller for sound2 is still present in the list.");
//
//                    Assert.DoesNotThrow(() => testInst.DetachSoundEffect(sound1), "Removing a second soundEffect failed");
//                    Assert.IsFalse(testInst.SoundEffectToController.ContainsKey(sound1), "The controller for sound1 is still present in the list.");
//
//                    Assert.IsTrue(testInst.SoundEffectToController.Keys.Count == 0, "There are some controller left in the component list.");
//
//                    // Check the exception thrwon by attachSoundEffect.
//
//                    Assert.Throws<ArgumentNullException>(() => testInst.AttachSoundEffect(null), "AttachSoundEffect did not throw ArgumentNullException");
//                    Assert.Throws<InvalidOperationException>(() => testInst.AttachSoundEffect(sound3), "AttachSoundEffect did not throw InvalidOperationException.");
//
//                    // Check the exception thrown by detachSoundEffect.
//
//                    Assert.Throws<ArgumentNullException>(() => testInst.DetachSoundEffect(null), "DetachSoundEffect did not throw ArgumentNullException.");
//                    Assert.Throws<ArgumentException>(() => testInst.DetachSoundEffect(sound1), "DetachSoundEffect did not throw ArgumentException ");
//                }
//            }
//        }
//
//        /// <summary>
//        /// Test the <see cref="AudioEmitterComponent.GetSoundEffectController"/> function.
//        /// </summary>
//        [Test]
//        public void TestGetController()
//        {
//            var testInst = new AudioEmitterComponent();
//            using (var game = new Game())
//            {
//                Game.InitializeAssetDatabase();
//                using (var audioStream1 = ContentManager.FileProvider.OpenStream("EffectToneA", VirtualFileMode.Open, VirtualFileAccess.Read))
//                using (var audioStream2 = ContentManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
//                {
//                    var sound1 = SoundEffect.Load(game.Audio.AudioEngine, audioStream1);
//                    var sound2 = SoundEffect.Load(game.Audio.AudioEngine, audioStream2);
//
//                    AudioEmitterSoundController soundController1 = null;
//                    AudioEmitterSoundController soundController2 = null;
//
//                    // Add two soundEffect and try to get their controller.
//
//                    testInst.AttachSoundEffect(sound1);
//                    Assert.DoesNotThrow(() => soundController1 = testInst.GetSoundEffectController(sound1), "Failed to get the controller associated to the soundEffect1.");
//
//                    testInst.AttachSoundEffect(sound2);
//                    Assert.DoesNotThrow(() => soundController2 = testInst.GetSoundEffectController(sound2), "Failed to get the controller associated to the soundEffect1.");
//
//                    // Suppress the two SoundEffects and check that the function throws the correct exception (ArgumentException)
//
//                    testInst.DetachSoundEffect(sound1);
//                    Assert.Throws<ArgumentException>(() => testInst.GetSoundEffectController(sound1), "GetController did not throw ArgumentException but sound1 was supposed to be detached.");
//
//                    testInst.DetachSoundEffect(sound2);
//                    Assert.Throws<ArgumentException>(() => testInst.GetSoundEffectController(sound2), "GetController did not throw ArgumentException but sound2 was supposed to be detached.");
//
//                    // Check the ArgumentNullException
//                    Assert.Throws<ArgumentNullException>(() => testInst.GetSoundEffectController(null), "GetController did not throw ArgumentNullException.");
//                }
//            }
//        }
//    }
//}
