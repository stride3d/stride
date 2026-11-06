// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Scripts;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for a <see cref="Method"/> inside a <see cref="VisualScriptAsset"/>.
/// </summary>
/// <remarks>
/// Slot regeneration (<c>RegenerateSlots</c> in the WPF view model) is not available here: it needs the Roslyn-based source code resolver of the script editor.
/// </remarks>
public class VisualScriptMethodViewModel : DispatcherViewModel
{
    private readonly VisualScriptViewModel visualScript;
    private readonly Method method;
    private readonly IObjectNode blocksContent;
    private readonly IObjectNode linksContent;
    private readonly IObjectNode parametersContent;
    private readonly MemberGraphNodeBinding<Accessibility> accessibilityNodeBinding;
    private readonly MemberGraphNodeBinding<VirtualModifier> virtualModifierNodeBinding;
    private readonly MemberGraphNodeBinding<bool> isStaticNodeBinding;
    private readonly MemberGraphNodeBinding<string> nameNodeBinding;
    private readonly MemberGraphNodeBinding<string> returnTypeNodeBinding;

    public VisualScriptMethodViewModel(VisualScriptViewModel visualScript, Method method)
        : base(visualScript.ServiceProvider)
    {
        this.visualScript = visualScript;
        this.method = method;
        var methodNode = visualScript.Session.AssetNodeContainer.GetOrCreateNode(method);
        blocksContent = methodNode[nameof(method.Blocks)].Target!;
        linksContent = methodNode[nameof(method.Links)].Target!;
        parametersContent = methodNode[nameof(method.Parameters)].Target!;

        // Create bindings
        accessibilityNodeBinding = new MemberGraphNodeBinding<Accessibility>(methodNode[nameof(method.Accessibility)], nameof(Accessibility), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
        virtualModifierNodeBinding = new MemberGraphNodeBinding<VirtualModifier>(methodNode[nameof(method.VirtualModifier)], nameof(VirtualModifier), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
        isStaticNodeBinding = new MemberGraphNodeBinding<bool>(methodNode[nameof(method.IsStatic)], nameof(IsStatic), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
        nameNodeBinding = new MemberGraphNodeBinding<string>(methodNode[nameof(method.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
        returnTypeNodeBinding = new MemberGraphNodeBinding<string>(methodNode[nameof(method.ReturnType)], nameof(ReturnType), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        accessibilityNodeBinding.Dispose();
        virtualModifierNodeBinding.Dispose();
        isStaticNodeBinding.Dispose();
        nameNodeBinding.Dispose();
        returnTypeNodeBinding.Dispose();

        base.Destroy();
    }

    public Accessibility Accessibility { get => accessibilityNodeBinding.Value; set => accessibilityNodeBinding.Value = value; }

    public VirtualModifier VirtualModifier { get => virtualModifierNodeBinding.Value; set => virtualModifierNodeBinding.Value = value; }

    public bool IsStatic { get => isStaticNodeBinding.Value; set => isStaticNodeBinding.Value = value; }

    public string Name { get => nameNodeBinding.Value; set => nameNodeBinding.Value = value; }

    public string ReturnType { get => returnTypeNodeBinding.Value; set => returnTypeNodeBinding.Value = value; }

    public Method Method => method;

    public VisualScriptViewModel VisualScript => visualScript;

    public void AddParameter(Parameter parameter)
    {
        parametersContent.Add(parameter);
    }

    public void RemoveParameter(Parameter parameter)
    {
        // Find index
        var index = method.Parameters.IndexOf(parameter);
        if (index < 0)
            return;

        // TODO: Cleanup references to this parameter

        // Remove
        var itemIndex = new NodeIndex(index);
        parametersContent.Remove(parameter, itemIndex);
    }

    public void AddBlock(Block block)
    {
        blocksContent.Add(block);
    }

    public void RemoveBlock(Block block)
    {
        // First, remove all links this block uses
        var i = 0;
        foreach (var link in method.Links.Values)
        {
            if (link.Source.Owner == block || link.Target.Owner == block)
            {
                var linkItemIndex = new NodeIndex(i);
                linksContent.Remove(link, linkItemIndex);

                // Since we removed an item, fix index of next check
                --i;
            }
        }

        // Remove
        var itemIndex = new NodeIndex(block.Id);
        blocksContent.Remove(block, itemIndex);
    }

    public void AddLink(Link link)
    {
        linksContent.Add(link);
    }

    public void RemoveLink(Link link)
    {
        // Remove
        var itemIndex = new NodeIndex(link.Id);
        linksContent.Remove(link, itemIndex);
    }
}
