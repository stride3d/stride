// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading;
using Stride.Core;
using Stride.Core.IO;
using Xunit;

using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Engine;
using Stride.Graphics.Font;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// Test the class <see cref="FontManager"/>
    /// </summary>
    public class TestFontManager
    {
        private void Init()
        {
            Game.InitializeAssetDatabase();
        }

        private const string SkipReasonAndroidReadOnlyData = "FontManager DB setup requires writable /data; on Android /data is the read-only APK mount";

        private IDatabaseFileProviderService CreateDatabaseProvider()
        {
            VirtualFileSystem.CreateDirectory(VirtualFileSystem.ApplicationDatabasePath);
            return new DatabaseFileProviderService(new DatabaseFileProvider(ObjectDatabase.CreateDefaultDatabase()));
        }

        [SkippableFact]
        public void TestCreationDisposal()
        {
            Skip.If(Platform.Type == PlatformType.Android, SkipReasonAndroidReadOnlyData);
            Init();

            var fontManager = new FontManager(CreateDatabaseProvider());
            fontManager.Dispose();
        }

        [SkippableFact]
        public void TestDoesFontContains()
        {
            Skip.If(Platform.Type == PlatformType.Android, SkipReasonAndroidReadOnlyData);
            Init();

            var fontManager = new FontManager(CreateDatabaseProvider());
            Assert.True(fontManager.DoesFontContains("Risaltyp_024", FontStyle.Regular, 'a'));
            Assert.False(fontManager.DoesFontContains("Risaltyp_024", FontStyle.Regular, '都'));
            fontManager.Dispose();
        }

        //Note: Test may fail due to some issues with SharpFont.
        //Updated TestGetFontInfo to now properly check if various Font Info is properly loaded
        [SkippableFact]
        public void TestGetFontInfo()
        {
            Skip.If(Platform.Type == PlatformType.Android, SkipReasonAndroidReadOnlyData);
            Init();

            var fontManager = new FontManager(CreateDatabaseProvider());

            float lineSpacing = 0f;
            float baseLine = 0f;
            float width = 0f;
            float height = 0f;

            fontManager.GetFontInfo("Risaltyp_024", FontStyle.Regular, out lineSpacing, out baseLine, out width, out height);
            Assert.Equal(4465f / 4096f, lineSpacing);
            Assert.Equal(3245f / 4096f, baseLine);
            Assert.Equal(3657f / 4096f, width);
            Assert.Equal(4075f / 4096f, height);

            fontManager.Dispose();
        }

        [SkippableFact]
        public void TestGenerateBitmap()
        {
            Skip.If(Platform.Type == PlatformType.Android, SkipReasonAndroidReadOnlyData);
            Init();

            var fontManager = new FontManager(CreateDatabaseProvider());
            const int waitTime = 250;
            const int defaultSize = 4;

            // test that a simple bitmap generation success
            var characterA = new CharacterSpecification('a', "Risaltyp_024", new Vector2(1.73f, 3.57f), FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterA, false);
            WaitAndCheck(characterA, waitTime);
            Assert.Equal(3, characterA.Bitmap.Width);
            Assert.Equal(3, characterA.Bitmap.Rows);
            
            // test that rendering an already existing character to a new size works
            var characterA2 = new CharacterSpecification('a', "Risaltyp_024", 10f * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterA2, false);
            WaitAndCheck(characterA2, waitTime);
            Assert.NotEqual(2, characterA2.Bitmap.Width);
            Assert.NotEqual(4, characterA2.Bitmap.Rows);

            // test that trying to render a character that does not exist does not crash the system
            var characterTo = new CharacterSpecification('都', "Risaltyp_024", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            var characterB = new CharacterSpecification('b', "Risaltyp_024", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterTo, false);
            fontManager.GenerateBitmap(characterB, false);
            WaitAndCheck(characterB, 2 * waitTime);
            Assert.Null(characterTo.Bitmap);

            // test that trying to render a character that does not exist does not crash the system
            var characterC = new CharacterSpecification('c', "Risaltyp_024", -1 * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            var characterD = new CharacterSpecification('d', "Risaltyp_024", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterC, false);
            fontManager.GenerateBitmap(characterD, false);
            WaitAndCheck(characterD, 2 * waitTime);
            Assert.Null(characterC.Bitmap);
            
            fontManager.Dispose();
        }

        private void WaitAndCheck(CharacterSpecification character, int sleepTime)
        {
            Thread.Sleep(sleepTime);
            Assert.NotNull(character.Bitmap);
        }
    }
}
