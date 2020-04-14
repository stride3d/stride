// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Metadata for a <see cref="Package"/> accessible from <see cref="PackageMeta"/>.
    /// </summary>
    [DataContract("PackageMeta")]
    public sealed class PackageMeta
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageMeta"/> class.
        /// </summary>
        public PackageMeta()
        {
            Authors = new List<string>();
            Owners = new List<string>();
        }

        /// <summary>
        /// Gets or sets the identifier name of this package.
        /// </summary>
        /// <value>The name.</value>
        [DataMember(10)]
        [DefaultValue(null)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of this package.
        /// </summary>
        /// <value>The version.</value>
        [DataMember(20)]
        [DefaultValue(null)]
        public PackageVersion Version { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [DataMember(30)]
        [DefaultValue(null)]
        public string Title { get; set; }

        /// <summary>
        /// Gets the authors.
        /// </summary>
        /// <value>The authors.</value>
        [DataMember(40)]
        public List<string> Authors { get; private set; }

        /// <summary>
        /// Gets the owners.
        /// </summary>
        /// <value>The owners.</value>
        [DataMember(50)]
        public List<string> Owners { get; private set; }

        /// <summary>
        /// Gets or sets the icon URL.
        /// </summary>
        /// <value>The icon URL.</value>
        [DataMember(60)]
        [DefaultValue(null)]
        public Uri IconUrl { get; set; }

        /// <summary>
        /// Gets or sets the license URL.
        /// </summary>
        /// <value>The license URL.</value>
        [DataMember(70)]
        [DefaultValue(null)]
        public Uri LicenseUrl { get; set; }

        /// <summary>
        /// Gets or sets the project URL.
        /// </summary>
        /// <value>The project URL.</value>
        [DataMember(80)]
        [DefaultValue(null)]
        public Uri ProjectUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it requires license acceptance.
        /// </summary>
        /// <value><c>true</c> if it requires license acceptance; otherwise, <c>false</c>.</value>
        [DataMember(90)]
        [DefaultValue(false)]
        public bool RequireLicenseAcceptance { get; set; }

        /// <summary>
        /// Gets or sets the description of this package.
        /// </summary>
        /// <value>The description.</value>
        [DataMember(100)]
        [DefaultValue(null)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the summary of this package.
        /// </summary>
        /// <value>The summary.</value>
        [DataMember(110)]
        [DefaultValue(null)]
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the release notes of this package.
        /// </summary>
        /// <value>The release notes.</value>
        [DataMember(120)]
        [DefaultValue(null)]
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// Gets or sets the language supported by this package.
        /// </summary>
        /// <value>The language.</value>
        [DataMember(130)]
        [DefaultValue(null)]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the tags associated to this package.
        /// </summary>
        /// <value>The tags.</value>
        [DataMember(140)]
        [DefaultValue(null)]
        public string Tags { get; set; }

        /// <summary>
        /// Gets or sets the copyright.
        /// </summary>
        /// <value>The copyright.</value>
        [DataMember(150)]
        [DefaultValue(null)]
        public string Copyright { get; set; }

        /// <summary>
        /// Gets or sets the default namespace for this package.
        /// </summary>
        /// <value>The default namespace.</value>
        [DataMember(155)]
        [DefaultValue(null)]
        public string RootNamespace { get; set; }

        /// <summary>
        /// Gets the package dependencies.
        /// </summary>
        /// <value>The package dependencies.</value>
        [DataMember(160)]
        public PackageDependencyCollection Dependencies { get; private set; }

        /// <summary>
        /// Gets the report abuse URL. Only valid for store packages.
        /// </summary>
        /// <value>The report abuse URL.</value>
        [DataMemberIgnore]
        public Uri ReportAbuseUrl { get; internal set; }

        /// <summary>
        /// Gets the download count. Only valid for store packages.
        /// </summary>
        /// <value>The download count.</value>
        [DataMemberIgnore]
        public long DownloadCount { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="PackageMeta"/> is listed.
        /// </summary>
        /// <value><c>true</c> if listed; otherwise, <c>false</c>.</value>
        [DataMemberIgnore]
        public bool Listed { get; internal set; }

        /// <summary>
        /// Gets the published time.
        /// </summary>
        /// <value>The published.</value>
        [DataMemberIgnore]
        public DateTimeOffset? Published { get; internal set; }

        /// <summary>
        /// Creates a new <see cref="PackageMeta" /> with default values.
        /// </summary>
        /// <param name="packageName">Name of the package.</param>
        /// <returns>PackageMeta.</returns>
        /// <exception cref="System.ArgumentNullException">packageName</exception>
        public static PackageMeta NewDefault(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName)) throw new ArgumentNullException("packageName");

            var meta = new PackageMeta()
                {
                    Name = packageName,
                    Version = new PackageVersion("1.0.0"),
                    Description = "Modify description of this package here",
                };
            meta.Authors.Add("Modify Author of this package here");

            return meta;
        }
    }
}
