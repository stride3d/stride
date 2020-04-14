// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.IO;

namespace Stride.ProjectGenerator
{
    /// <summary>
    /// Create a package.
    /// </summary>
    public class PackageUnitTestGenerator : TemplateGeneratorBase<SessionTemplateGeneratorParameters>
    {
        public static readonly PackageUnitTestGenerator Default = new PackageUnitTestGenerator();

        public static readonly Guid TemplateId = new Guid("3c4ac35f-4d63-462e-9696-974ebaa9a862");

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

        public sealed override bool Run(SessionTemplateGeneratorParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();

            var name = parameters.Name;
            var outputDirectory = parameters.OutputDirectory;

            // Creates the package
            var package = NewPackage(name);

            // Setup the default namespace
            package.Meta.RootNamespace = parameters.Namespace;

            // Setup the path to save it
            package.FullPath = UPath.Combine(outputDirectory, new UFile(name + Package.PackageFileExtension));

            // Add it to the current session
            var session = parameters.Session;
            session.Projects.Add(new StandalonePackage(package));

            // Load missing references
            session.LoadMissingReferences(parameters.Logger);
            return true;
        }

        /// <summary>
        /// Creates a new Stride package with the specified name
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

            return package;
        }
    }
}
