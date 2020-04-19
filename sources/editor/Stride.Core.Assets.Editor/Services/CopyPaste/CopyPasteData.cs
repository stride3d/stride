// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Represents copied data.
    /// </summary>
    /// <remarks>
    /// Copied data is serialized in YAML before being stored in the clipboard.
    /// </remarks>
    [DataContract(nameof(CopyPasteData))]
    internal class CopyPasteData
    {
        [DataMember(0)]
        [NotNull]
        public string ItemType { get; set; }

        /// <summary>
        /// The collection of the copied items.
        /// </summary>
        [DataMember]
        [NonIdentifiableCollectionItems]
        [ItemNotNull, NotNull]
        public List<CopyPasteItem> Items { get; } = new List<CopyPasteItem>();

        /// <summary>
        /// Gets the collection of overridden members in the copied data.
        /// </summary>
        /// <remarks>Properties that are not in this dictionary are considered to have the <see cref="OverrideType.Base"/> type.</remarks>
        [DataMemberIgnore]
        public YamlAssetMetadata<OverrideType> Overrides { get; set; }
    }

    /// <summary>
    /// Represents an item of copied data.
    /// </summary>
    [DataContract(nameof(CopyPasteItem))]
    internal class CopyPasteItem
    {
        /// <summary>
        /// The actual copied data.
        /// </summary>
        [DataMember]
        [NotNull]
        public object Data { get; set; }

        /// <summary>
        /// The identifier of the source object, if it has one.
        /// </summary>
        /// <renakrs>The source object could be a container of the copied data, e.g. the container asset.</renakrs>
        [DataMember]
        [DefaultValue(null)]
        public AssetId? SourceId { get; set; }

        /// <summary>
        /// Indicates if the root value of <see cref="Data"/> is an object reference or not.
        /// </summary>
        [DataMemberIgnore]
        public bool IsRootObjectReference { get; set; }
    }
}
