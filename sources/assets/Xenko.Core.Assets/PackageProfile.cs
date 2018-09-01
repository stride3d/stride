// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Settings;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// Describes buld parameters used when building assets.
    /// </summary>
    [DataContract("PackageProfile")]
    public sealed class PackageProfile
    {
        public static SettingsContainer SettingsContainer = new SettingsContainer();

        private readonly AssetFolderCollection assetFolders;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageProfile"/> class.
        /// </summary>
        public PackageProfile()
        {
            assetFolders = new AssetFolderCollection();
            OutputGroupDirectories = new Dictionary<string, UDirectory>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageProfile" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="folders">The folders.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public PackageProfile(params AssetFolder[] folders)
            : this()
        {
            foreach (var folder in folders)
            {
                AssetFolders.Add(folder);
            }
        }

        /// <summary>
        /// Gets or sets the platform.
        /// </summary>
        /// <value>The platform.</value>
        [DataMember(20)]
        public PlatformType Platform { get; set; }

        /// <summary>
        /// Gets the asset directories to lookup.
        /// </summary>
        /// <value>The asset directories.</value>
        [DataMember(40)]
        public AssetFolderCollection AssetFolders => assetFolders;

        /// <summary>
        /// Gets the resource directories to lookup.
        /// </summary>
        /// <value>The resource directories.</value>
        [DataMember(45)]
        public List<UDirectory> ResourceFolders { get; } = new List<UDirectory>();

        /// <summary>
        /// Gets the output group directories.
        /// </summary>
        /// <value>The output group directories.</value>
        [DataMember(50)]
        public Dictionary<string, UDirectory> OutputGroupDirectories { get; private set; }

        /// <summary>
        /// Creates a a default shared package profile.
        /// </summary>
        /// <returns>PackageProfile.</returns>
        public static PackageProfile NewShared()
        {
            var sharedProfile = new PackageProfile() { Platform = PlatformType.Shared };
            sharedProfile.AssetFolders.Add(new AssetFolder("Assets"));
            sharedProfile.ResourceFolders.Add("Resources");
            return sharedProfile;
        }
    }
}
