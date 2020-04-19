using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Yaml;

namespace Stride.Core.Assets.Quantum
{
    /// <summary>
    /// A class containing helper methods for asset cloning.
    /// </summary>
    public static class AssetCloningHelper
    {
        /// <summary>
        /// Updates the paths in the given <see cref="YamlAssetMetadata{T}"/> instance to reflect that new <see cref="Guid"/> have been generated after cloning,
        /// when <see cref="AssetClonerFlags.GenerateNewIdsForIdentifiableObjects"/> has been used.
        /// </summary>
        /// <typeparam name="T">The type of content in the metadata.</typeparam>
        /// <param name="metadata">The metadata to update.</param>
        /// <param name="idRemapping">A dictionary representing the mapping between initial ids and their corresponding id in the cloned object.</param>
        /// <param name="basePath">If not null, this method will apply the remapping only for paths that are contained in the given base path.</param>
        public static void RemapIdentifiablePaths<T>(YamlAssetMetadata<T> metadata, Dictionary<Guid, Guid> idRemapping, YamlAssetPath basePath = null)
        {
            // Early exit if nothing to remap
            if (metadata == null || idRemapping == null)
                return;

            var replacements = new List<Tuple<YamlAssetPath, YamlAssetPath, T>>();
            foreach (var entry in metadata)
            {
                // Skip paths that doesn't start with the given base path.
                if (basePath != null && !entry.Key.StartsWith(basePath))
                    continue;

                var newPath = new YamlAssetPath(entry.Key.Elements.Select(x => FixupIdentifier(x, idRemapping)));
                replacements.Add(Tuple.Create(entry.Key, newPath, entry.Value));
            }

            // First remove everything, then re-add everything, in case we have a collision between an old path and a new path
            foreach (var replacement in replacements)
            {
                metadata.Remove(replacement.Item1);
            }
            foreach (var replacement in replacements)
            {
                metadata.Set(replacement.Item2, replacement.Item3);
            }
        }

        private static YamlAssetPath.Element FixupIdentifier(YamlAssetPath.Element element, Dictionary<Guid, Guid> idRemapping)
        {
            switch (element.Type)
            {
                case YamlAssetPath.ElementType.Index:
                    if (element.Value is Guid && idRemapping.TryGetValue((Guid)element.Value, out var newId))
                    {
                        return new YamlAssetPath.Element(YamlAssetPath.ElementType.Index, newId);
                    }
                    return element;
                case YamlAssetPath.ElementType.Member:
                case YamlAssetPath.ElementType.ItemId:
                    return element;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}