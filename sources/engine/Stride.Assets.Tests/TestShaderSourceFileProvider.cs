// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Stride.Assets.Effect;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Shaders.Compilers;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Tests that <see cref="ShaderSourceFileProvider"/> serves the shader files a <see cref="ShaderSourceLocator"/> finds as their import
    /// would have stored them, so that the effect compiler reads them where they are.
    /// </summary>
    public sealed class TestShaderSourceFileProvider : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Tests", Guid.NewGuid().ToString("N"));
        private readonly FileSystemProvider database;

        public TestShaderSourceFileProvider()
        {
            Directory.CreateDirectory(Path.Combine(directory, "db", "shaders"));
            File.WriteAllText(Path.Combine(directory, "db", "shaders", "Imported.sdsl"), "shader Imported {}");
            database = new FileSystemProvider(null, Path.Combine(directory, "db"));
        }

        public void Dispose()
        {
            database.Dispose();
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
                // A file the system still holds stays in the temporary folder
            }
        }

        [Fact]
        public void TheLastShaderWithAFileNameWins()
        {
            var first = CreatePackage("First", ("Foo", "shader Foo { float first; }"));
            var second = CreatePackage("Second", ("Foo", "shader Foo { float second; }"), ("Bar", "shader Bar {}"));

            var locator = ShaderSourceLocator.Create([first, second]);

            Assert.True(locator.TryGetFilePath("Foo.sdsl", out var foo));
            Assert.Equal(second.Assets.Find("Foo")!.FullPath, foo);
            Assert.Equal(new[] { "Bar.sdsl", "Foo.sdsl" }, locator.FileNames.Order());
            Assert.Equal(new[] { "First", "Second" }, locator.Packages.Select(x => x.Name));
        }

        [Fact]
        public void AnUpdateNamesTheShadersWhoseFileChanged()
        {
            var first = CreatePackage("First", ("Foo", "shader Foo {}"), ("Bar", "shader Bar {}"));
            var second = CreatePackage("Second", ("Foo", "shader Foo {}"));
            var locator = ShaderSourceLocator.Create([first]);

            // Foo now comes from the second package, Bar is gone, Baz is new
            first.Assets.Remove(first.Assets.Find("Bar")!);
            AddShader(first, "Baz", "shader Baz {}");
            var changed = locator.Update([first, second]);

            Assert.Equal(new[] { "Bar.sdsl", "Baz.sdsl", "Foo.sdsl" }, changed.Order());
            Assert.Empty(locator.Update([first, second]));
        }

        [Fact]
        public void TheShaderFilesAreServedAsTheirImportStoresThem()
        {
            const string source = "shader Foo { float value; }";
            var package = CreatePackage("Game", ("Foo", source));
            var filePath = package.Assets.Find("Foo")!.FullPath;
            using var provider = new ShaderSourceFileProvider(ShaderSourceLocator.Create([package]), database);

            Assert.True(provider.FileExists("shaders/Foo.sdsl"));
            Assert.Equal(source, ReadText(provider, "shaders/Foo.sdsl"));
            Assert.Equal(Encoding.UTF8.GetByteCount(source), provider.FileSize("shaders/Foo.sdsl"));

            // The path the import stores next to the file, which the compiler and hot reload read on Windows
            Assert.True(provider.FileExists("shaders/Foo.sdsl/path"));
            Assert.Equal(filePath.FullPath, ReadText(provider, "shaders/Foo.sdsl/path"));

            // Hot reload watches the file itself
            Assert.True(provider.TryGetFileLocation("shaders/Foo.sdsl", out var location, out var start, out _));
            Assert.Equal(filePath.ToOSPath(), location);
            Assert.Equal(0, start);
        }

        [Fact]
        public void EverythingElseComesFromTheDatabase()
        {
            var package = CreatePackage("Game", ("Foo", "shader Foo {}"));
            AddShader(package, "NotSaved", null);
            using var provider = new ShaderSourceFileProvider(ShaderSourceLocator.Create([package]), database);

            Assert.Equal("shader Imported {}", ReadText(provider, "shaders/Imported.sdsl"));
            // A shader asset whose file isn't on the disk yet isn't there
            Assert.False(provider.FileExists("shaders/NotSaved.sdsl"));
            Assert.False(provider.FileExists("shaders/Missing.sdsl"));
            Assert.False(provider.FileExists("other/Foo.sdsl"));

            var files = provider.ListFiles("shaders", "*.sdsl", VirtualSearchOption.TopDirectoryOnly);
            Assert.Contains("shaders/Foo.sdsl", files);
            Assert.Contains(files, x => x.EndsWith("Imported.sdsl", StringComparison.Ordinal));
            Assert.DoesNotContain("shaders/NotSaved.sdsl", files);
        }

        [Fact]
        public void TheCompilerLoadsAShaderWithTheHashOfItsFile()
        {
            // A BOM, as some editors write: the hash is the one of the bytes of the file, as with an import
            var package = CreatePackage("Game", ("Foo", "﻿shader Foo { float value; }"));
            var filePath = package.Assets.Find("Foo")!.FullPath.ToOSPath();
            using var provider = new ShaderSourceFileProvider(ShaderSourceLocator.Create([package]), database);
            var manager = new ShaderSourceManager(provider);
            manager.LookupDirectoryList.Add("shaders");

            var shader = manager.LoadShaderSource("Foo");

            Assert.Equal(ObjectId.FromBytes(File.ReadAllBytes(filePath)), shader.Hash);
            Assert.Equal("shader Foo { float value; }", shader.Source);
            if (Platform.IsWindowsDesktop)
                Assert.Equal(Path.GetFullPath(filePath), Path.GetFullPath(shader.Path));
            Assert.True(manager.IsClassExists("Imported"));
            Assert.False(manager.IsClassExists("Missing"));
        }

        private Package CreatePackage(string name, params (string Location, string Source)[] shaders)
        {
            var package = new Package { FullPath = new UFile(Path.Combine(directory, name, name + Package.PackageFileExtension)), Meta = { Name = name } };
            foreach (var (location, source) in shaders)
                AddShader(package, location, source);
            return package;
        }

        private static void AddShader(Package package, string location, string? source)
        {
            var assetItem = new AssetItem(location, new EffectShaderAsset());
            package.Assets.Add(assetItem);
            if (source is null)
                return;

            var filePath = assetItem.FullPath.ToOSPath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(source));
        }

        private static string ReadText(IVirtualFileProvider provider, string url)
        {
            using var stream = provider.OpenStream(url, VirtualFileMode.Open, VirtualFileAccess.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
