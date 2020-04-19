// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    public class BlockTemplateDescriptionViewModel : TemplateDescriptionViewModel
    {
        public BlockTemplateDescriptionViewModel(IViewModelServiceProvider serviceProvider, IBlockFactory blockFactory) : base(serviceProvider, CreateTemplate(blockFactory))
        {
            BlockFactory = blockFactory;
        }

        public IBlockFactory BlockFactory { get; }

        public static TemplateDescription CreateTemplate(IBlockFactory blockFactory)
        {
            return new TemplateDescription
            {
                Id = Guid.NewGuid(),
                Group = blockFactory.Category,
                Name = blockFactory.Name,
            };
        }
    }

    public class BlockTemplateDescriptionCollectionViewModel : AddItemTemplateCollectionViewModel
    {
        public BlockTemplateDescriptionCollectionViewModel(VisualScriptEditorViewModel editor) : base(editor.ServiceProvider)
        {
            // Create a template for each non-abstract block type
            foreach (var blockFactory in AssemblyRegistry.FindAll().SelectMany(x => x.GetTypes())
                .Where(t => !t.IsAbstract && typeof(Block).IsAssignableFrom(t) && t != typeof(MethodCallBlock) && t.GetConstructor(Type.EmptyTypes) != null)
                .Where(t => TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<ObsoleteAttribute>(t) == null)
                .Where(t => TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<NonInstantiableAttribute>(t) == null)
                .Select(t => new BlockFromType(t)))
            {
                RegisterBlockTemplate(blockFactory);
            }

            // Use BlockFromMethod for MethodCallBlock
            RegisterBlockTemplate(new BlockFromMethod(editor));

            SelectedGroup = RootGroup;
        }

        private void RegisterBlockTemplate(BlockFromType blockFactory)
        {
            var group = ProcessGroup(RootGroup, blockFactory.Category);
            if (group != null)
            {
                var viewModel = new BlockTemplateDescriptionViewModel(ServiceProvider, blockFactory);
                group.Templates.Add(viewModel);
            }
        }
    }

    public interface IBlockFactory
    {
        string Name { get; }

        string Category { get; }

        Task<Block> Create();
    }

    public class BlockFromMethod : BlockFromType
    {
        private static readonly SymbolDisplayFormat UnqualifiedNameOnlyFormat =
            new SymbolDisplayFormat();

        private static readonly SymbolDisplayFormat QualifiedNameOnlyFormat =
            new SymbolDisplayFormat(
                SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

        private readonly VisualScriptEditorViewModel editor;

        public BlockFromMethod(VisualScriptEditorViewModel editor) : base(typeof(MethodCallBlock))
        {
            this.editor = editor;
        }

        public override async Task<Block> Create()
        {
            // Ask user which method (and overload)
            var methodSymbol = await editor.AskUserForSymbol() as IMethodSymbol;
            if (methodSymbol == null)
                return null;
            
            var result = (MethodCallBlock)await base.Create();
            result.IsMemberCall = !methodSymbol.IsStatic || methodSymbol.IsExtensionMethod;
            result.MethodName = methodSymbol.ToDisplayString(result.IsMemberCall ? UnqualifiedNameOnlyFormat : QualifiedNameOnlyFormat);

            // Generate default input/output slots
            result.GenerateSlots(result.Slots, new SlotGeneratorContext());

            // Add "this" as slot (if not static)
            if (!methodSymbol.IsStatic)
                result.Slots.Add(new Slot(SlotDirection.Input, SlotKind.Value, "this", type: methodSymbol.ContainingType?.ToDisplayString(QualifiedNameOnlyFormat)));

            // TODO: ref/out parameters
            // Add parameters as slot
            foreach (var parameter in methodSymbol.Parameters)
                result.Slots.Add(new Slot(SlotDirection.Input, SlotKind.Value, parameter.Name, type: parameter.Type?.ToDisplayString(QualifiedNameOnlyFormat)));

            // Add return slot (if not void)
            if (!methodSymbol.ReturnsVoid)
                result.Slots.Add(new Slot(SlotDirection.Output, SlotKind.Value, type: methodSymbol.ReturnType?.ToDisplayString(QualifiedNameOnlyFormat)));

            return result;
        }
    }

    public class BlockFromType : IBlockFactory
    {
        public BlockFromType(Type blockType)
        {
            if (!typeof(Block).IsAssignableFrom(blockType))
                throw new ArgumentException($"The given type does not derive from {nameof(Block)}");

            Type = blockType;
            Category = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<Core.DisplayAttribute>(Type)?.Category ?? "Misc";
        }

        public string Name => Type.Name;

        public Type Type { get; }

        public string Category { get; }

        public virtual Task<Block> Create()
        {
            return Task.FromResult((Block)Activator.CreateInstance(Type));
        }
    }
}
