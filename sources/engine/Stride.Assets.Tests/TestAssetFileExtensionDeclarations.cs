// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Stride.Core.Assets;
using Stride.Core.Serialization;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The runtime assemblies declare the extension and content type of every engine asset type
    /// ([assembly: AssetFileExtension], read by the asset URL constants generator), and declare nothing else.
    /// </summary>
    public class TestAssetFileExtensionDeclarations
    {
        [Fact]
        public void RuntimeDeclarationsMatchTheAssetTypes()
        {
            var assetAssemblies = new[]
            {
                typeof(Textures.TextureAsset).Assembly,
                typeof(Models.ModelAsset).Assembly,
                typeof(SpriteStudio.Offline.SpriteStudioModelAsset).Assembly,
                typeof(BepuPhysics.Assets.HullAsset).Assembly,
                typeof(Video.Assets.VideoAsset).Assembly,
            };

            var expected = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var assembly in assetAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var contentType = type.GetCustomAttribute<AssetContentTypeAttribute>();
                    if (contentType == null)
                        continue;
                    var description = type.GetCustomAttribute<AssetDescriptionAttribute>();
                    if (description == null)
                        continue;
                    foreach (var extension in description.FileExtensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        expected.Add($"{extension}|{contentType.ContentType.FullName}");
                }
            }

            // Declared by the runtime assemblies the asset assemblies reference, where a game finds them; a reference
            // that does not load here (a build tool such as Microsoft.Build) declares none
            var actual = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var reference in assetAssemblies.SelectMany(x => x.GetReferencedAssemblies()).DistinctBy(x => x.Name))
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.Load(reference);
                }
                catch (Exception e) when (e is FileNotFoundException or FileLoadException)
                {
                    continue;
                }
                foreach (var declaration in assembly.GetCustomAttributes<AssetFileExtensionAttribute>())
                    actual.Add($"{declaration.Extension}|{declaration.ContentType.FullName}");
            }

            if (!expected.SetEquals(actual))
            {
                var message = new StringBuilder();
                message.AppendLine("[assembly: AssetFileExtension] declarations are out of sync with the [AssetDescription]/[AssetContentType] asset types.");
                foreach (var line in expected.Except(actual))
                    message.AppendLine($"  missing: {line}");
                foreach (var line in actual.Except(expected))
                    message.AppendLine($"  stale:   {line}");
                Assert.Fail(message.ToString());
            }
        }
    }
}
