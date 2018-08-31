// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.IO;

namespace Xenko.Assets.Presentation.Templates
{
    /// <summary>
    /// Create a package.
    /// </summary>
    public class NewPackageTemplateGenerator : SessionTemplateGenerator
    {
        public static readonly NewPackageTemplateGenerator Default = new NewPackageTemplateGenerator();

        public static readonly Guid TemplateId = new Guid("548eedd2-d014-486f-988e-b2f1aad02341");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == TemplateId;
        }

        public override Task<bool> PrepareForRun(SessionTemplateGeneratorParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();
            return Task.FromResult(true);
        }

        protected override bool Generate(SessionTemplateGeneratorParameters parameters)
        {
            return GeneratePackage(parameters) != null;
        }

        public static Package GeneratePackage(SessionTemplateGeneratorParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();

            var name = Utilities.BuildValidNamespaceName(parameters.Name);
            var outputDirectory = parameters.OutputDirectory;

            // Creates the package
            var package = NewPackage(name);

            // Setup the default namespace
            package.Meta.RootNamespace = parameters.Namespace;

            // Setup the path to save it
            package.FullPath = UPath.Combine(outputDirectory, new UFile(name + Package.PackageFileExtension));

            // Add it to the current session
            var session = parameters.Session;
            session.Packages.Add(package);

            // Load missing references
            session.LoadMissingReferences(parameters.Logger);

            return package;
        }

        /// <summary>
        /// Creates a new Xenko package with the specified name
        /// </summary>
        /// <param name="name">Name of the package</param>
        /// <returns>A new package instance</returns>
        public static Package NewPackage(string name)
        {
            var package = new Package
                {
                    Meta =
                        {
                            Name = name,
                            Version = new PackageVersion("1.0.0.0")
                        },
                };

            // Add dependency to latest Xenko package
            package.Meta.Dependencies.Add(XenkoConfig.GetLatestPackageDependency());

            return package;
        }
    }
}
