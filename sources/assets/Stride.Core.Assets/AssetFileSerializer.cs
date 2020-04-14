// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core.Assets.Serializers;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets
{
    public class AssetLoadResult<T>
    {
        public AssetLoadResult(T asset, ILogger logger, bool aliasOccurred, AttachedYamlAssetMetadata yamlMetadata)
        {
            if (yamlMetadata == null) throw new ArgumentNullException(nameof(yamlMetadata));
            Asset = asset;
            Logger = logger;
            AliasOccurred = aliasOccurred;
            YamlMetadata = yamlMetadata;
        }

        public T Asset { get; }

        public ILogger Logger { get; }

        public bool AliasOccurred { get; }

        public AttachedYamlAssetMetadata YamlMetadata { get; }
    }

    /// <summary>
    /// Main entry point for serializing/deserializing <see cref="Asset"/>.
    /// </summary>
    public static class AssetFileSerializer
    {
        private static readonly List<IAssetSerializerFactory> RegisteredSerializerFactories = new List<IAssetSerializerFactory>();

        /// <summary>
        /// The default serializer.
        /// </summary>
        public static readonly IAssetSerializer Default = new YamlAssetSerializer();

        static AssetFileSerializer()
        {
            Register((YamlAssetSerializer)Default);
            Register(SourceCodeAssetSerializer.Default);
        }

        /// <summary>
        /// Registers the specified serializer factory.
        /// </summary>
        /// <param name="serializerFactory">The serializer factory.</param>
        /// <exception cref="System.ArgumentNullException">serializerFactory</exception>
        public static void Register(IAssetSerializerFactory serializerFactory)
        {
            if (serializerFactory == null) throw new ArgumentNullException(nameof(serializerFactory));
            if (!RegisteredSerializerFactories.Contains(serializerFactory))
                RegisteredSerializerFactories.Add(serializerFactory);
        }

        /// <summary>
        /// Finds a serializer for the specified asset file extension.
        /// </summary>
        /// <param name="assetFileExtension">The asset file extension.</param>
        /// <returns>IAssetSerializerFactory.</returns>
        public static IAssetSerializer FindSerializer(string assetFileExtension)
        {
            if (assetFileExtension == null) throw new ArgumentNullException(nameof(assetFileExtension));
            assetFileExtension = assetFileExtension.ToLowerInvariant();
            for (int i = RegisteredSerializerFactories.Count - 1; i >= 0; i--)
            {
                var assetSerializerFactory = RegisteredSerializerFactories[i];
                var factory = assetSerializerFactory.TryCreate(assetFileExtension);
                if (factory != null)
                {
                    return factory;
                }
            }
            return null;
        }

        /// <summary>
        /// Deserializes an <see cref="Asset"/> from the specified stream.
        /// </summary>
        /// <typeparam name="T">Type of the asset</typeparam>
        /// <param name="filePath">The file path.</param>
        /// <param name="log">The logger.</param>
        /// <returns>An instance of Asset not a valid asset asset object file.</returns>
        public static AssetLoadResult<T> Load<T>(string filePath, ILogger log = null)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var result = Load<T>(stream, filePath, log);
                return result;
            }
        }

        public static AssetLoadResult<T> Load<T>(Stream stream, UFile filePath, ILogger log = null)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            var assetFileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            var serializer = FindSerializer(assetFileExtension);
            if (serializer == null)
            {
                throw new InvalidOperationException("Unable to find a serializer for [{0}]".ToFormat(assetFileExtension));
            }
            bool aliasOccurred;
            AttachedYamlAssetMetadata yamlMetadata;
            var asset = (T)serializer.Load(stream, filePath, log, true, out aliasOccurred, out yamlMetadata);
            return new AssetLoadResult<T>(asset, log, aliasOccurred, yamlMetadata);
        }

        /// <summary>
        /// Serializes an <see cref="Asset" /> to the specified file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="asset">The asset object.</param>
        /// <param name="yamlMetadata"></param>
        /// <param name="log">The logger.</param>
        /// <exception cref="System.ArgumentNullException">filePath</exception>
        public static void Save(string filePath, object asset, AttachedYamlAssetMetadata yamlMetadata, ILogger log = null)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            // Creates automatically the directory when saving an asset.
            filePath = FileUtility.GetAbsolutePath(filePath);
            var directoryPath = Path.GetDirectoryName(filePath);
            if (directoryPath != null && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (var stream = new MemoryStream())
            {
                Save(stream, asset, yamlMetadata, log);
                File.WriteAllBytes(filePath, stream.ToArray());
            }
        }

        /// <summary>
        /// Serializes an <see cref="Asset" /> to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="asset">The asset object.</param>
        /// <param name="yamlMetadata"></param>
        /// <param name="log">The logger.</param>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// assetFileExtension
        /// </exception>
        public static void Save(Stream stream, object asset, AttachedYamlAssetMetadata yamlMetadata, ILogger log = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (asset == null) return;

            var assetFileExtension = AssetRegistry.GetDefaultExtension(asset.GetType());
            if (assetFileExtension == null)
            {
                throw new ArgumentException("Unable to find a serializer for the specified asset. No asset file extension registered to AssetRegistry");
            }

            var serializer = FindSerializer(assetFileExtension);
            if (serializer == null)
            {
                throw new InvalidOperationException($"Unable to find a serializer for [{assetFileExtension}]");
            }
            serializer.Save(stream, asset, yamlMetadata, log);
        }
    }
}
