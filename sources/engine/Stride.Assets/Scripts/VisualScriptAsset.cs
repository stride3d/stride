// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Serializers;
using Stride.Core;
using Stride.Core.Collections;

namespace Stride.Assets.Scripts
{
    [DataContract("VisualScriptAsset")]
    [AssetDescription(FileExtension)]
    public partial class VisualScriptAsset : AssetComposite, IProjectFileGeneratorAsset
    {
        /// <summary>
        /// The default file extension used by the <see cref="VisualScriptAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdvs";

        [DataMember(0)]
        [DefaultValue(Accessibility.Public)]
        public Accessibility Accessibility { get; set; } = Accessibility.Public;

        [DataMember(10)]
        [DefaultValue(false)]
        public bool IsStatic { get; set; }

        [DataMember(20)]
        public string BaseType { get; set; }

        [DataMember(30)]
        public string Namespace { get; set; }

        /// <summary>
        /// The list of using directives.
        /// </summary>
        [DataMember(40)]
        public TrackingCollection<string> UsingDirectives { get; set; } = new TrackingCollection<string>();

        /// <summary>
        /// The list of member variables (properties and fields).
        /// </summary>
        [DataMember(50)]
        [AssetPartContained(typeof(Property))]
        public TrackingCollection<Property> Properties { get; } = new TrackingCollection<Property>();

        /// <summary>
        /// The list of functions.
        /// </summary>
        [DataMember(60)]
        [AssetPartContained(typeof(Method))]
        public TrackingCollection<Method> Methods { get; } = new TrackingCollection<Method>();

        #region IProjectFileGeneratorAsset implementation

        [DataMember(Mask = DataMemberAttribute.IgnoreMask)]
        [Display(Browsable = false)]
        public string Generator { get; } = "StrideVisualScriptGenerator";

        #endregion

        /// <inheritdoc/>
        [Obsolete("The AssetPart struct might be removed soon")]
        public override IEnumerable<AssetPart> CollectParts()
        {
            foreach (var member in Properties)
                yield return new AssetPart(member.Id, member.Base, newBase => member.Base = newBase);
            foreach (var function in Methods)
            {
                yield return new AssetPart(function.Id, function.Base, newBase => function.Base = newBase);
                foreach (var parmeter in function.Parameters)
                    yield return new AssetPart(parmeter.Id, parmeter.Base, newBase => parmeter.Base = newBase);
                foreach (var block in function.Blocks.Values)
                    yield return new AssetPart(block.Id, block.Base, newBase => block.Base = newBase);
                foreach (var link in function.Links.Values)
                    yield return new AssetPart(link.Id, link.Base, newBase => link.Base = newBase);
            }
        }

        /// <inheritdoc/>
        public override bool ContainsPart(Guid id)
        {
            foreach (var variable in Properties)
            {
                if (variable.Id == id)
                    return true;
            }

            foreach (var method in Methods)
            {
                if (method.Id == id)
                    return true;

                if (method.Blocks.ContainsKey(id) || method.Links.ContainsKey(id))
                    return true;

                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Id == id)
                        return true;
                }

                foreach (var block in method.Blocks.Values)
                {
                    foreach (var slot in block.Slots)
                    {
                        if (slot.Id == id)
                            return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override IIdentifiable FindPart(Guid id)
        {
            foreach (var variable in Properties)
            {
                if (variable.Id == id)
                    return variable;
            }

            foreach (var method in Methods)
            {
                if (method.Id == id)
                    return method;

                Block matchingBlock;
                if (method.Blocks.TryGetValue(id, out matchingBlock))
                    return matchingBlock;

                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Id == id)
                        return parameter;
                }

                foreach (var block in method.Blocks.Values)
                {
                    foreach (var slot in block.Slots)
                    {
                        if (slot.Id == id)
                            return slot;
                    }
                }
            }

            return null;
        }

        public void SaveGeneratedAsset(AssetItem assetItem)
        {
            var generatedAbsolutePath = assetItem.GetGeneratedAbsolutePath();

            var compilerResult = Compile(assetItem);
            File.WriteAllText(generatedAbsolutePath, compilerResult.GeneratedSource);
        }

        public VisualScriptCompilerResult Compile(AssetItem assetItem)
        {
            var generatedAbsolutePath = assetItem.GetGeneratedAbsolutePath().ToWindowsPath();

            var compilerOptions = new VisualScriptCompilerOptions
            {
                FilePath = generatedAbsolutePath,
                Class = Path.GetFileNameWithoutExtension(generatedAbsolutePath),
            };

            // Try to get root namespace from containing project
            // Since ProjectReference.Location is sometimes absolute sometimes not, we have to handle both case
            // TODO: ideally we should stop converting those and handle this automatically in a custom Yaml serializer?
            var sourceProjectAbsolute = (assetItem.Package.Container as SolutionProject)?.FullPath;
            var sourceProjectRelative = sourceProjectAbsolute?.MakeRelative(assetItem.Package.FullPath.GetFullDirectory());

            if (sourceProjectAbsolute != null)
            {
                // Find root namespace from project
                var rootNamespace = assetItem.Package.RootNamespace ?? sourceProjectAbsolute.GetFileName();
                if (rootNamespace != null)
                {
                    compilerOptions.DefaultNamespace = rootNamespace;

                    // Complete namespace with "Include" folder (if not empty)
                    var projectInclude = assetItem.GetProjectInclude();
                    if (projectInclude != null)
                    {
                        var lastDirectorySeparator = projectInclude.LastIndexOf('\\');
                        if (lastDirectorySeparator != -1)
                        {
                            var projectIncludeFolder = projectInclude.Substring(0, lastDirectorySeparator);
                            compilerOptions.DefaultNamespace += '.' + projectIncludeFolder.Replace('\\', '.');
                        }
                    }
                }
            }

            var compilerResult = VisualScriptCompiler.Generate(this, compilerOptions);
            return compilerResult;
        }
    }
}
