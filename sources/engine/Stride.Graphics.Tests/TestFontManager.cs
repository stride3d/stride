// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading;
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

        private IDatabaseFileProviderService CreateDatabaseProvider()
        {
            VirtualFileSystem.CreateDirectory(VirtualFileSystem.ApplicationDatabasePath);
            return new DatabaseFileProviderService(new DatabaseFileProvider(ObjectDatabase.CreateDefaultDatabase()));
        }

        [Fact]
        public void TestCreationDisposal()
        {
            Init();

            var fontManager = new FontManager(CreateDatabaseProvider());
            fontManager.Dispose();
        }
        
        [Fact]
        public void TestDoesFontContains()
        {
            Init();

            var fontManager = new FontManager(CreateDatabaseProvider());
            Assert.True(fontManager.DoesFontContains("Arial", FontStyle.Regular, 'a'));
            Assert.False(fontManager.DoesFontContains("Arial", FontStyle.Regular, '都'));
            fontManager.Dispose();
        }

        [Fact]
        public void TestGetFontInfo()
        {
            Init();

            var fontManager = new FontManager(CreateDatabaseProvider());

            float lineSpacing = 0;
            float baseLine = 0;
            float width = 0;
            float height = 0;
            fontManager.GetFontInfo("Risaltyp_024", FontStyle.Regular, out lineSpacing, out baseLine, out width, out height);
            Assert.Equal(4444f / 4096f, lineSpacing);
            Assert.Equal(3233f / 4096f, baseLine);
            Assert.Equal(3657f / 4096f, width);
            Assert.Equal(4075f / 4096f, height);

            fontManager.Dispose();
        }

        [Fact]
        public void TestGenerateBitmap()
        {
            Init();

            var fontManager = new FontManager(CreateDatabaseProvider());
            const int waitTime = 250;
            const int defaultSize = 4;

            // test that a simple bitmap generation success
            var characterA = new CharacterSpecification('a', "Arial", new Vector2(1.73f, 3.57f), FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterA, false);
            WaitAndCheck(characterA, waitTime);
            Assert.Equal(4, characterA.Bitmap.Width);
            Assert.Equal(6, characterA.Bitmap.Rows);
            
            // test that rendering an already existing character to a new size works
            var characterA2 = new CharacterSpecification('a', "Arial", 10f * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterA2, false);
            WaitAndCheck(characterA2, waitTime);
            Assert.NotEqual(2, characterA2.Bitmap.Width);
            Assert.NotEqual(4, characterA2.Bitmap.Rows);

            // test that trying to render a character that does not exist does not crash the system
            var characterTo = new CharacterSpecification('都', "Arial", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            var characterB = new CharacterSpecification('b', "Arial", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterTo, false);
            fontManager.GenerateBitmap(characterB, false);
            WaitAndCheck(characterB, 2 * waitTime);
            Assert.Null(characterTo.Bitmap);

            // test that trying to render a character that does not exist does not crash the system
            var characterC = new CharacterSpecification('c', "Arial", -1 * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            var characterD = new CharacterSpecification('d', "Arial", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
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
