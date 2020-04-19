// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Stride.Core.Packages
{
    /// <summary>
    /// Copy of <see cref="CachingSourceProvider"/> from Nuget with the only change being adding V2 in the list 
    /// of resource providers.
    /// </summary>
    internal class NugetSourceRepositoryProvider : ISourceRepositoryProvider
    {
        private readonly List<Lazy<INuGetResourceProvider>> _resourceProviders;
        private readonly List<SourceRepository> _repositories;

        // There should only be one instance of the source repository for each package source.
        private static readonly ConcurrentDictionary<PackageSource, SourceRepository> _cachedSources
            = new ConcurrentDictionary<PackageSource, SourceRepository>();

        public NugetSourceRepositoryProvider(IPackageSourceProvider packageSourceProvider, INugetDownloadProgress downloadProgress)
        {
            PackageSourceProvider = packageSourceProvider;

            _resourceProviders = new List<Lazy<INuGetResourceProvider>>();
            _resourceProviders.AddRange(Repository.Provider.GetCoreV3());

            // Create repositories
            _repositories = PackageSourceProvider.LoadPackageSources()
                .Where(s => s.IsEnabled)
                .Select(CreateRepository)
                .ToList();
        }

        /// <inheritdoc cref="ISourceRepositoryProvider"/>
        public IEnumerable<SourceRepository> GetRepositories()
        {
            return _repositories;
        }

        /// <inheritdoc cref="ISourceRepositoryProvider"/>
        public SourceRepository CreateRepository(PackageSource source)
        {
            var feedTypeSource = source as FeedTypePackageSource;
            return CreateRepository(source, feedTypeSource?.FeedType ?? FeedType.Undefined);
        }

        /// <inheritdoc cref="ISourceRepositoryProvider"/>
        public SourceRepository CreateRepository(PackageSource source, FeedType type)
        {
            return _cachedSources.GetOrAdd(source, new SourceRepository(source, _resourceProviders, type));
        }

        /// <inheritdoc cref="ISourceRepositoryProvider"/>
        public IPackageSourceProvider PackageSourceProvider { get; }
    }
}
