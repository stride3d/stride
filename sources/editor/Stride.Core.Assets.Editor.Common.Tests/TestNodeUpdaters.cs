// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Common.Tests;

public class TestNodeUpdaters
{
    [DataContract]
    [Display("Pretty type")]
    public class Described
    {
        [Display("Pretty member", Expand = ExpandRule.Always)]
        public string? Member { get; set; }

        public string? Plain { get; set; }
    }

    [DataContract]
    public class Inlined
    {
        [InlineProperty(Expand = ExpandRule.Once)]
        public float Value { get; set; }

        public float Other { get; set; }
    }

    [DataContract]
    public class Paths
    {
        public UFile? File { get; set; }

        public UDirectory? Directory { get; set; }

        public string? Text { get; set; }

        public Inlined Inlined { get; set; } = new();

        public Toggle Toggle { get; set; } = new();
    }

    [DataContract]
    public class Toggle
    {
        public bool Enabled { get; set; }
    }

    [Fact]
    public void DisplayAttributeOnMemberSetsNameAndExpandRule()
    {
        var root = new PresenterTestContext().CreatePresenter(new Described());
        var member = PresenterTestContext.Child(root, nameof(Described.Member));
        var plain = PresenterTestContext.Child(root, nameof(Described.Plain));

        var updater = new DisplayAttributeNodeUpdater();
        updater.UpdateNode(member);
        updater.UpdateNode(plain);

        Assert.Equal("Pretty member", member.DisplayName);
        Assert.Equal(ExpandRule.Always, member.AttachedProperties.Get(DisplayData.AutoExpandRuleKey));
        Assert.Equal(nameof(Described.Plain), plain.DisplayName);
        Assert.False(plain.AttachedProperties.ContainsKey(DisplayData.AutoExpandRuleKey));
    }

    [Fact]
    public void DisplayAttributeOnTypeSetsAttributeDisplayName()
    {
        var root = new PresenterTestContext().CreatePresenter(new Described());

        new DisplayAttributeNodeUpdater().UpdateNode(root);

        Assert.Equal("Pretty type", root.AttachedProperties.Get(DisplayData.AttributeDisplayNameKey));
    }

    [Fact]
    public void UPathMembersGetAReferenceViewModel()
    {
        var root = new PresenterTestContext().CreatePresenter(new Paths());
        var updater = new UPathNodeUpdater();
        foreach (var child in root.Children)
            updater.UpdateNode(child);

        Assert.IsType<UPathReferenceViewModel>(PresenterTestContext.Child(root, nameof(Paths.File)).AttachedProperties.Get(ReferenceData.Key));
        Assert.IsType<UPathReferenceViewModel>(PresenterTestContext.Child(root, nameof(Paths.Directory)).AttachedProperties.Get(ReferenceData.Key));
        Assert.False(PresenterTestContext.Child(root, nameof(Paths.Text)).AttachedProperties.ContainsKey(ReferenceData.Key));
    }

    [Fact]
    public void InlinePropertyIsRenamedAndHidden()
    {
        var root = new PresenterTestContext().CreatePresenter(new Paths());

        new InlineMemberNodeUpdater().FinalizeTree(root);

        var inlined = PresenterTestContext.Child(root, nameof(Paths.Inlined));
        Assert.True(inlined.AttachedProperties.Get(InlineData.InlineMemberKey));
        Assert.Equal(ExpandRule.Once, inlined.AttachedProperties.Get(DisplayData.AutoExpandRuleKey));
        var inlinedValue = PresenterTestContext.Child(inlined, InlineData.InlinedProperty);
        Assert.False(inlinedValue.IsVisible);
        Assert.True(PresenterTestContext.Child(inlined, nameof(Inlined.Other)).IsVisible);
    }

    [Fact]
    public void EnabledPropertiesAreHidden()
    {
        var root = new PresenterTestContext().CreatePresenter(new Paths());

        new InlineMemberNodeUpdater().FinalizeTree(root);

        var toggle = PresenterTestContext.Child(root, nameof(Paths.Toggle));
        Assert.False(PresenterTestContext.Child(toggle, nameof(Toggle.Enabled)).IsVisible);
    }

    [Fact]
    public void CategoryNodeNameIsDerivedFromCategory()
    {
        Assert.Equal("RenderingCategory", CategoryData.ComputeCategoryNodeName("Rendering"));
    }
}
