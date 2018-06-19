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
    /// A collection of <see cref="PackageProfile"/>.
    /// </summary>
    [DataContract("PackageProfileCollection")]
    public sealed class PackageProfileCollection : KeyedCollection<string, PackageProfile>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageProfileCollection"/> class.
        /// </summary>
        public PackageProfileCollection() : base(null, -1)
        {
            // Set -1 to dictionary threshold in order to make sure we don't use a dictionary internaly (so that
            // renaming PackageProfile will work)
        }

        protected override string GetKeyForItem(PackageProfile item)
        {
            return item.Name;
        }

        /// <summary>
        /// Finds a shared profile (a profile that is not platform specific)
        /// </summary>
        /// <returns>PackageProfile.</returns>
        public PackageProfile FindSharedProfile()
        {
            return this.FirstOrDefault(profile => profile.Platform == PlatformType.Shared);
        }
    }

    /// <summary>
    /// Describes buld parameters used when building assets.
    /// </summary>
    [DataContract("PackageProfile")]
    public sealed class PackageProfile
    {
        public static SettingsContainer SettingsContainer = new SettingsContainer();

        private readonly AssetFolderCollection assetFolders;

        public const string SharedName = "Shared";

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageProfile"/> class.
        /// </summary>
        public PackageProfile()
        {
            assetFolders = new AssetFolderCollection();
            InheritProfiles = new List<string>();
            OutputGroupDirectories = new Dictionary<string, UDirectory>();
            ProjectReferences = new List<ProjectReference>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageProfile"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public PackageProfile(string name) : this()
        {
            if (name == null) throw new ArgumentNullException("name");
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageProfile" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="folders">The folders.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public PackageProfile(string name, params AssetFolder[] folders)
            : this()
        {
            if (name == null) throw new ArgumentNullException("name");
            Name = name;
            foreach (var folder in folders)
            {
                AssetFolders.Add(folder);
            }
        }


        /// <summary>
        /// Gets or sets the name of this profile.
        /// </summary>
        /// <value>The name.</value>
        [DataMember(10)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the platform.
        /// </summary>
        /// <value>The platform.</value>
        [DataMember(20)]
        public PlatformType Platform { get; set; }

        /// <summary>
        /// Gets the inherit profiles.
        /// </summary>
        /// <value>The inherit profiles.</value>
        [DataMember(30)]
        public List<string> InheritProfiles { get; private set; }

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
        /// Gets the assembly references to load when compiling this package.
        /// </summary>
        /// <value>The assembly references.</value>
        [DataMember(70)]
        public List<ProjectReference> ProjectReferences { get; private set; }

        /// <summary>
        /// Creates a a default shared package profile.
        /// </summary>
        /// <returns>PackageProfile.</returns>
        public static PackageProfile NewShared()
        {
            var sharedProfile = new PackageProfile(SharedName) { Platform = PlatformType.Shared };
            sharedProfile.AssetFolders.Add(new AssetFolder("Assets/" + SharedName));
            sharedProfile.ResourceFolders.Add("Resources");
            return sharedProfile;
        }
    }
}
