// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public class MemberNodePresenter : NodePresenterBase
{
    protected readonly IMemberNode Member;
    private readonly List<Attribute> memberAttributes = [];

    public MemberNodePresenter(INodePresenterFactoryInternal factory, IPropertyProviderViewModel? propertyProvider, INodePresenter parent, IMemberNode member)
        : base(factory, propertyProvider, parent)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(parent);
        Member = member ?? throw new ArgumentNullException(nameof(member));
        Name = member.Name;
        CombineKey = Name;
        DisplayName = Name;
        IsReadOnly = !Member.MemberDescriptor.HasSet;
        memberAttributes.AddRange(TypeDescriptorFactory.Default.AttributeRegistry.GetAttributes(member.MemberDescriptor.MemberInfo));

        member.ValueChanging += OnMemberChanging;
        member.ValueChanged += OnMemberChanged;

        if (member.Target != null)
        {
            member.Target.ItemChanging += OnItemChanging;
            member.Target.ItemChanged += OnItemChanged;
        }
        var displayAttribute = memberAttributes.OfType<DisplayAttribute>().FirstOrDefault();
        Order = displayAttribute?.Order ?? member.MemberDescriptor.Order;

        AttachCommands();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Member.ValueChanging -= OnMemberChanging;
            Member.ValueChanged -= OnMemberChanged;
            if (Member.Target != null)
            {
                Member.Target.ItemChanging -= OnItemChanging;
                Member.Target.ItemChanged -= OnItemChanged;
            }
        }
    }

    public override Type Type => Member.Type;

    public override bool IsEnumerable => Member.Target?.IsEnumerable ?? false;

    public override NodeIndex Index => NodeIndex.Empty;

    public override ITypeDescriptor Descriptor => Member.Descriptor;

    public override object Value => Member.Retrieve();

    public IMemberDescriptor MemberDescriptor => Member.MemberDescriptor;

    public IReadOnlyList<Attribute> MemberAttributes => memberAttributes;

    protected override IObjectNode ParentingNode => Member.Target;

    public override void UpdateValue(object newValue)
    {
        // Do not update member node presenter value to null if it does not allow null values (related to issue #668).
        // FIXME With the obsoleting of Stride.Core.Annotations.NotNullAttribute, it might become partially broken.
        //       Non-null members are no-longer annotated. 
        //
        //       What are the failing use cases? Should we just check for value types here?
        //       We could also decide to keep (non obsolete) NotNullAttribute just for that purpose.
        //       For now, check for our NotNullAttribute as well as from CodeAnalysis
        if ((newValue == null) && memberAttributes.Any(x => x is Annotations.NotNullAttribute or System.Diagnostics.CodeAnalysis.NotNullAttribute))
            return;

        try
        {
            Member.Update(newValue);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while updating the value of the node, see the inner exception for more information.", e);
        }
    }

    public override void AddItem(object value)
    {
        if (Member.Target == null || !Member.Target.IsEnumerable)
            throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

        try
        {
            Member.Target.Add(value);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
        }
    }

    public override void AddItem(object value, NodeIndex index)
    {
        if (Member.Target == null || !Member.Target.IsEnumerable)
            throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

        try
        {
            Member.Target.Add(value, index);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
        }
    }

    public override void RemoveItem(object value, NodeIndex index)
    {
        if (Member.Target == null || !Member.Target.IsEnumerable)
            throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(RemoveItem)} cannot be invoked on members that are not collection.");

        try
        {
            Member.Target.Remove(value, index);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while removing an item to the node, see the inner exception for more information.", e);
        }
    }

    public override NodeAccessor GetNodeAccessor()
    {
        return new NodeAccessor(Member, NodeIndex.Empty);
    }

    private void OnMemberChanging(object? sender, MemberNodeChangeEventArgs e)
    {
        RaiseValueChanging(Value);
        if (Member.Target != null)
        {
            Member.Target.ItemChanging -= OnItemChanging;
            Member.Target.ItemChanged -= OnItemChanged;
        }
    }

    private void OnMemberChanged(object? sender, MemberNodeChangeEventArgs e)
    {
        Refresh();
        if (Member.Target != null)
        {
            Member.Target.ItemChanging += OnItemChanging;
            Member.Target.ItemChanged += OnItemChanged;
        }
        RaiseValueChanged(Value);
    }

    private void OnItemChanging(object? sender, ItemChangeEventArgs e)
    {
        RaiseValueChanging(Value);
    }

    private void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        Refresh();
        RaiseValueChanged(Value);
    }
}
