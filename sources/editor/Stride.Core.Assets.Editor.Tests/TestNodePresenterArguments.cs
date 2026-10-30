// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The argument checks of the WPF node presenters that the agnostic copies took over.
/// </summary>
public sealed class TestNodePresenterArguments
{
    public enum Kind
    {
        First,
    }

    public sealed class Numbers
    {
        public int Value { get; set; }
    }

    [Fact]
    public void AnAbstractTypeGroupNeedsAName()
    {
        AbstractNodeType[] types = [new(typeof(Numbers))];

        Assert.Throws<ArgumentNullException>(() => new AbstractNodeTypeGroup(null!, types));
        Assert.Throws<ArgumentNullException>(() => new AbstractNodeTypeGroup(string.Empty, types));
        Assert.Equal("Group", new AbstractNodeTypeGroup("Group", types).GroupName);
    }

    [Theory]
    [InlineData(typeof(sbyte), sbyte.MinValue, sbyte.MaxValue)]
    [InlineData(typeof(int), int.MinValue, int.MaxValue)]
    [InlineData(typeof(ulong), ulong.MinValue, ulong.MaxValue)]
    [InlineData(typeof(float), float.MinValue, float.MaxValue)]
    [InlineData(typeof(double), double.MinValue, double.MaxValue)]
    public void NumericTypesHaveTheirBounds(Type type, object minimum, object maximum)
    {
        Assert.Equal(minimum, NumericValueNodeUpdater.GetMinimum(type));
        Assert.Equal(maximum, NumericValueNodeUpdater.GetMaximum(type));
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(char))]
    [InlineData(typeof(Kind))]
    [InlineData(typeof(int?))]
    public void OtherTypesHaveNoBounds(Type type)
    {
        // As in WPF, the bounds of a type that is not numeric are an error, not a value
        Assert.Throws<ArgumentException>(() => NumericValueNodeUpdater.GetMinimum(type));
        Assert.Throws<ArgumentException>(() => NumericValueNodeUpdater.GetMaximum(type));
    }

    [Fact]
    public void TheNumericUpdaterNeedsAMember()
    {
        var node = new PresenterTestContext().CreatePresenter(new Numbers());
        var value = PresenterTestContext.Child(node, nameof(Numbers.Value));

        Assert.Throws<ArgumentNullException>(() => new NumericValueNodeUpdater().UpdateNode(value, (MemberInfo)null!));
    }
}
