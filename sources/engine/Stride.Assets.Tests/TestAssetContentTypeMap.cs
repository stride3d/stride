// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Stride.Core;
using Stride.Core.Assets;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Keeps the checked-in asset tag -> content type map (consumed by the asset URL constants
    /// generator) in sync with the engine's [AssetContentType] declarations.
    /// </summary>
    public class TestAssetContentTypeMap
    {
        [Fact]
        public void EngineMapMatchesAssetContentTypeDeclarations()
        {
            var assetAssemblies = new[]
            {
                typeof(Textures.TextureAsset).Assembly,
                typeof(Models.ModelAsset).Assembly,
                typeof(SpriteStudio.Offline.SpriteStudioModelAsset).Assembly,
            };

            var expected = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var assembly in assetAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var contentType = type.GetCustomAttribute<AssetContentTypeAttribute>();
                    if (contentType == null)
                        continue;
                    var tag = type.GetCustomAttribute<DataContractAttribute>()?.Alias ?? type.Name;
                    expected.Add($"{tag}|{contentType.ContentType.FullName}");
                }
            }

            var mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Stride.AssetContentTypeMap.txt");
            var actual = new SortedSet<string>(
                File.ReadAllLines(mapPath).Select(line => line.Trim()).Where(line => line.Length > 0 && line[0] != '#'),
                StringComparer.Ordinal);

            if (!expected.SetEquals(actual))
            {
                var message = new StringBuilder();
                message.AppendLine("Stride.AssetContentTypeMap.txt is out of sync with [AssetContentType] declarations.");
                foreach (var line in expected.Except(actual))
                    message.AppendLine($"  missing: {line}");
                foreach (var line in actual.Except(expected))
                    message.AppendLine($"  stale:   {line}");
                Assert.Fail(message.ToString());
            }
        }
    }
}
