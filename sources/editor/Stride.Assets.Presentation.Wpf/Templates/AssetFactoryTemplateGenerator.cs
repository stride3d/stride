// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Reflection;

namespace Stride.Assets.Presentation.Templates
{
    public class AssetFactoryTemplateGenerator : AssetTemplateGenerator
    {
        private static readonly PropertyKey<Dictionary<TemplateAssetPrompt, object>> PromptValuesKey = new PropertyKey<Dictionary<TemplateAssetPrompt, object>>("PromptValues", typeof(AssetFactoryTemplateGenerator));

        public static readonly AssetFactoryTemplateGenerator Default = new AssetFactoryTemplateGenerator();

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription is TemplateAssetFactoryDescription;
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            var values = new Dictionary<TemplateAssetPrompt, object>();
            parameters.SetTag(PromptValuesKey, values);

            if (parameters.Description is not TemplateAssetFactoryDescription desc || desc.Prompts.Count == 0)
                return true;

            var assetType = desc.GetAssetType();
            foreach (var prompt in desc.Prompts)
            {
                // The member is set on the created asset (its type may only be known then, through a list of shapes for instance); check the first segment exists now
                GetMember(assetType, prompt.Member.Split('.', '[')[0]);
                switch (prompt)
                {
                    case AssetReferencePrompt assetPrompt:
                        var acceptedTypes = assetPrompt.AssetTypes.Select(ResolveType).ToList();
                        values[prompt] = await BrowseForAsset(parameters.Package, acceptedTypes, new UFile(parameters.Name).GetFullDirectory(), assetPrompt.Message);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported template prompt [{prompt.GetType().Name}].");
                }
            }
            return true;
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var desc = parameters.Description as TemplateAssetFactoryDescription;
            if (desc == null)
                yield break;

            var factory = desc.GetFactory();
            if (factory == null)
                throw new InvalidOperationException("Unable to find the asset factory associated to this template.");

            var asset = factory.New();
            yield return new AssetItem(GenerateLocation(parameters), asset);
        }

        protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
        {
            base.PostAssetCreation(parameters, assetItem);
            // Set by PrepareAssetCreation, which an override may skip
            if (parameters.GetTag(PromptValuesKey) is not { } values)
                return;

            foreach (var (prompt, value) in values)
            {
                if (value == null)
                    continue;

                var (target, member) = ResolveMember(assetItem.Asset, prompt.Member);
                switch (prompt)
                {
                    case AssetReferencePrompt:
                        member.Set(target, ContentReferenceHelper.CreateReference((AssetViewModel)value, member.Type));
                        break;
                }
            }
        }

        private static IMemberDescriptor GetMember(Type type, string memberName)
        {
            return TypeDescriptorFactory.Default.Find(type).TryGetMember(memberName)
                ?? throw new InvalidOperationException($"Unable to find member [{memberName}] on type [{type.Name}].");
        }

        /// <summary>
        /// Walks a member path such as <c>ColliderShapes[0].Model</c> on <paramref name="root"/> and returns the object
        /// holding the last member with that member's descriptor.
        /// </summary>
        internal static (object target, IMemberDescriptor member) ResolveMember(object root, string path)
        {
            var segments = path.Split('.');
            var current = root;
            foreach (var segment in segments[..^1])
            {
                var (name, index) = ParseSegment(segment);
                current = GetMember(current.GetType(), name).Get(current) ?? throw new InvalidOperationException($"[{name}] is null while resolving [{path}].");
                if (index != null)
                    current = ((IList)current)[index.Value] ?? throw new InvalidOperationException($"[{name}[{index}]] is null while resolving [{path}].");
            }

            var (lastName, lastIndex) = ParseSegment(segments[^1]);
            if (lastIndex != null)
                throw new InvalidOperationException($"A prompt cannot set a list element ([{path}]).");
            return (current, GetMember(current.GetType(), lastName));
        }

        private static (string name, int? index) ParseSegment(string segment)
        {
            var bracket = segment.IndexOf('[');
            return bracket < 0 ? (segment, null) : (segment[..bracket], int.Parse(segment[(bracket + 1)..^1]));
        }

        private static Type ResolveType(string typeName)
        {
            if (typeName.Contains(','))
                return AssemblyRegistry.GetType(typeName);

            return AssetRegistry.GetPublicTypes().FirstOrDefault(x => x.Name == typeName)
                ?? AssemblyRegistry.FindAll().SelectMany(GetExportedTypes).FirstOrDefault(x => x.Name == typeName)
                ?? throw new InvalidOperationException($"Unable to find type [{typeName}].");
        }

        private static IEnumerable<Type> GetExportedTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetExportedTypes();
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}
