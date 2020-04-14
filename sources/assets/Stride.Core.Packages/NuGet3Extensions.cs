// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using NuGet.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGetManifestFile = NuGet.Packaging.ManifestFile;
using NuGetManifestMetadata = NuGet.Packaging.ManifestMetadata;

namespace Xenko.Core.Packages
{
    public static class NuGet3Extensions
    {
        /// <summary>
        /// Converts a <see cref="VersionRange"/> into a <see cref="PackageVersionRange"/>.
        /// </summary>
        /// <param name="range">The source of conversion.</param>
        /// <returns>A new instance of <see cref="PackageVersionRange"/> corresponding to <paramref name="range"/>.</returns>
        public static PackageVersionRange ToPackageVersionRange(this VersionRange range)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));

            return new PackageVersionRange(range.MinVersion?.ToPackageVersion(), range.IsMinInclusive, range.MaxVersion?.ToPackageVersion(), range.IsMaxInclusive);
        }

        /// <summary>
        /// Converts a <see cref="NuGetVersion"/> into a <see cref="PackageVersion"/>.
        /// </summary>
        /// <param name="version">The source of conversion.</param>
        /// <returns>A new instance of <see cref="PackageVersion"/> corresponding to <paramref name="version"/>.</returns>
        public static PackageVersion ToPackageVersion(this NuGetVersion version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            return new PackageVersion(version.Version, version.Release);
        }

        /// <summary>
        /// Converts a <see cref="PackageVersionRange"/> into a <see cref="VersionRange"/>.
        /// </summary>
        /// <param name="range">The source of conversion.</param>
        /// <returns>A new instance of <see cref="VersionRange"/> corresponding to <paramref name="range"/>.</returns>
        public static VersionRange ToVersionRange(this PackageVersionRange range)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));

            return new VersionRange(range.MinVersion?.ToNuGetVersion(), range.IsMinInclusive, range.MaxVersion?.ToNuGetVersion(), range.IsMaxInclusive);
        }

        /// <summary>
        /// Converts a <see cref="PackageVersion"/> into a <see cref="NuGetVersion"/>.
        /// </summary>
        /// <param name="version">The source of conversion.</param>
        /// <returns>A new instance of <see cref="NuGetVersion"/> corresponding to <paramref name="version"/>.</returns>
        public static NuGetVersion ToNuGetVersion(this PackageVersion version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            return new NuGetVersion(version.Version, version.SpecialVersion);
        }

        /// <summary>
        /// Converts a <see cref="ManifestFile"/> into a <see cref="NuGetManifestFile"/>.
        /// </summary>
        /// <param name="file">The manifest file source of conversion.</param>
        /// <returns>A new instance of <see cref="NuGetManifestFile"/> corresponding to <paramref name="file"/>.</returns>
        public static NuGetManifestFile ToManifestFile(this ManifestFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            return new NuGetManifestFile()
            {
                Source = file.Source,
                Exclude = file.Exclude,
                Target = file.Target
            };
        }

        /// <summary>
        /// Converts a <see cref="ManifestMetadata"/> into a <see cref="NuGetManifestMetadata"/>.
        /// </summary>
        /// <param name="metadata">The metadata source of conversion.</param>
        /// <returns>A new instance of <see cref="NuGetManifestMetadata"/> corresponding to <paramref name="metadata"/>.</returns>
        public static NuGetManifestMetadata ToManifestMetadata(this ManifestMetadata metadata)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var nugetMetadata = new NuGetManifestMetadata()
            {
                Id = metadata.Id,
                Authors = metadata.Authors,
                Description = metadata.Description,
                Copyright = metadata.Copyright,
                DevelopmentDependency = metadata.DevelopmentDependency,
                Version = new NuGetVersion(metadata.Version),
                Owners = metadata.Owners,
                Language = metadata.Language,
                MinClientVersionString = metadata.MinClientVersionString,
                ReleaseNotes = metadata.ReleaseNotes,
                RequireLicenseAcceptance = metadata.RequireLicenseAcceptance,
                Summary = metadata.Summary,
                Tags = metadata.Tags,
                Title = metadata.Title
            };
            // Setting properties without a setter.
            nugetMetadata.SetIconUrl(metadata.IconUrl);
            nugetMetadata.SetLicenseUrl(metadata.LicenseUrl);
            nugetMetadata.SetProjectUrl(metadata.ProjectUrl);

            // Updating dependencies
            if (metadata.Dependencies.Count != 0)
            {
                var packages = new List<PackageDependency>();
                foreach (var dependency in metadata.Dependencies)
                {
                    packages.Add(new PackageDependency(dependency.Id, dependency.Version.ToVersionRange()));
                }
                // We are .NET agnostic
                var group = new PackageDependencyGroup(NuGetFramework.AgnosticFramework, packages);
                nugetMetadata.DependencyGroups = new [] {group};
            }

            return nugetMetadata;
        }
    }
}
