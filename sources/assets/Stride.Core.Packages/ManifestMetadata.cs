// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Packages
{
    public class ManifestMetadata
    {
        public ManifestMetadata()
        {
            Dependencies = new List<ManifestDependency>();
        }

        public string MinClientVersionString { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public string Title { get; set; }
        public IEnumerable<string> Authors { get; set; }
        public IEnumerable<string> Owners { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string IconUrl { get; set; }
        public bool RequireLicenseAcceptance { get; set; }
        public bool DevelopmentDependency { get; set; }
        public string Description { get; set; }
        public string Summary { get; set; }
        public string ReleaseNotes { get; set; }
        public string Copyright { get; set; }
        public string Language { get; set; }
        public string Tags { get; set; }
        public List<ManifestDependency> Dependencies { get; set; }

        /// <summary>
        /// Add new dependency to package name <paramref name="name"/> with version <paramref name="v"/> to
        /// the first set if it exists already, otherwise create a new sets where dependency will be added to.
        /// </summary>
        /// <param name="name">Name of package to add to <see cref="Dependencies"/></param>
        /// <param name="v">Version range accepted for package to add to <see cref="Dependencies"/></param>
        public void AddDependency(string name, PackageVersionRange v)
        {
            Dependencies.Add(new ManifestDependency() { Id = name, Version = v });
        }
    }
}
