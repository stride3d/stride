// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Reflection;
using Stride.Core.TypeConverters;
using Xunit;

namespace Stride.Core.Design.Tests.TypeConverters;

/// <summary>
/// Tests for <see cref="FieldPropertyDescriptor"/> class.
/// </summary>
public class TestFieldPropertyDescriptor
{
    // Test class with various field types
    private class TestClass
    {
        public int PublicField = 42;
        public string? StringField = "test";
        public readonly int ReadOnlyField = 100;

        [Description("Test Description")]
        [Browsable(false)]
        public double AnnotatedField = 3.14;
    }

    [Fact]
    public void Constructor_WithValidFieldInfo_SetsProperties()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.NotNull(descriptor);
        Assert.Equal(fieldInfo, descriptor.FieldInfo);
        Assert.Equal(nameof(TestClass.PublicField), descriptor.Name);
        Assert.Equal(typeof(int), descriptor.PropertyType);
        Assert.Equal(typeof(TestClass), descriptor.ComponentType);
    }

    [Fact]
    public void IsReadOnly_AlwaysReturnsFalse()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.False(descriptor.IsReadOnly);
    }

    [Fact]
    public void IsReadOnly_WithReadOnlyField_StillReturnsFalse()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.ReadOnlyField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        // FieldPropertyDescriptor always returns false for IsReadOnly
        Assert.False(descriptor.IsReadOnly);
    }

    [Fact]
    public void PropertyType_ReturnsFieldType()
    {
        var intFieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var intDescriptor = new FieldPropertyDescriptor(intFieldInfo);
        Assert.Equal(typeof(int), intDescriptor.PropertyType);

        var stringFieldInfo = typeof(TestClass).GetField(nameof(TestClass.StringField))!;
        var stringDescriptor = new FieldPropertyDescriptor(stringFieldInfo);
        Assert.Equal(typeof(string), stringDescriptor.PropertyType);
    }

    [Fact]
    public void ComponentType_ReturnsDeclaringType()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.Equal(typeof(TestClass), descriptor.ComponentType);
    }

    [Fact]
    public void GetValue_ReturnsFieldValue()
    {
        var testObj = new TestClass();
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        var value = descriptor.GetValue(testObj);

        Assert.Equal(42, value);
    }

    [Fact]
    public void GetValue_WithNullComponent_ThrowsTargetException()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        // Non-static fields require a target object
        Assert.Throws<TargetException>(() => descriptor.GetValue(null));
    }

    [Fact]
    public void SetValue_UpdatesFieldValue()
    {
        var testObj = new TestClass();
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        descriptor.SetValue(testObj, 99);

        Assert.Equal(99, testObj.PublicField);
    }

    [Fact]
    public void SetValue_TriggersValueChangedEvent()
    {
        var testObj = new TestClass();
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        bool eventTriggered = false;
        descriptor.AddValueChanged(testObj, (sender, e) => eventTriggered = true);

        descriptor.SetValue(testObj, 99);

        Assert.True(eventTriggered);
    }

    [Fact]
    public void CanResetValue_AlwaysReturnsFalse()
    {
        var testObj = new TestClass();
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.False(descriptor.CanResetValue(testObj));
    }

    [Fact]
    public void ResetValue_DoesNothing()
    {
        var testObj = new TestClass { PublicField = 123 };
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        descriptor.ResetValue(testObj);

        // Value should remain unchanged
        Assert.Equal(123, testObj.PublicField);
    }

    [Fact]
    public void ShouldSerializeValue_AlwaysReturnsTrue()
    {
        var testObj = new TestClass();
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.True(descriptor.ShouldSerializeValue(testObj));
    }

    [Fact]
    public void Constructor_PreservesFieldAttributes()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.AnnotatedField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        var descriptionAttr = descriptor.Attributes.OfType<DescriptionAttribute>().FirstOrDefault();
        Assert.NotNull(descriptionAttr);
        Assert.Equal("Test Description", descriptionAttr.Description);

        var browsableAttr = descriptor.Attributes.OfType<BrowsableAttribute>().FirstOrDefault();
        Assert.NotNull(browsableAttr);
        Assert.False(browsableAttr.Browsable);
    }

    [Fact]
    public void Equals_WithSameFieldInfo_ReturnsTrue()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor1 = new FieldPropertyDescriptor(fieldInfo);
        var descriptor2 = new FieldPropertyDescriptor(fieldInfo);

        Assert.True(descriptor1.Equals(descriptor2));
        Assert.True(descriptor1.Equals((object)descriptor2));
    }

    [Fact]
    public void Equals_WithDifferentFieldInfo_ReturnsFalse()
    {
        var fieldInfo1 = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var fieldInfo2 = typeof(TestClass).GetField(nameof(TestClass.StringField))!;
        var descriptor1 = new FieldPropertyDescriptor(fieldInfo1);
        var descriptor2 = new FieldPropertyDescriptor(fieldInfo2);

        Assert.False(descriptor1.Equals(descriptor2));
        Assert.False(descriptor1.Equals((object)descriptor2));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.False(descriptor.Equals(null));
        Assert.False(descriptor.Equals((object?)null));
    }

    [Fact]
    public void Equals_WithSelf_ReturnsTrue()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.True(descriptor.Equals(descriptor));
        Assert.True(descriptor.Equals((object)descriptor));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.False(descriptor.Equals("not a descriptor"));
        Assert.False(descriptor.Equals(42));
    }

    [Fact]
    public void GetHashCode_WithSameFieldInfo_ReturnsSameHashCode()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor1 = new FieldPropertyDescriptor(fieldInfo);
        var descriptor2 = new FieldPropertyDescriptor(fieldInfo);

        Assert.Equal(descriptor1.GetHashCode(), descriptor2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentFieldInfo_ReturnsDifferentHashCode()
    {
        var fieldInfo1 = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var fieldInfo2 = typeof(TestClass).GetField(nameof(TestClass.StringField))!;
        var descriptor1 = new FieldPropertyDescriptor(fieldInfo1);
        var descriptor2 = new FieldPropertyDescriptor(fieldInfo2);

        // Hash codes should be different (though not guaranteed, very likely)
        Assert.NotEqual(descriptor1.GetHashCode(), descriptor2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_WithSameFieldInfo_ReturnsTrue()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor1 = new FieldPropertyDescriptor(fieldInfo);
        var descriptor2 = new FieldPropertyDescriptor(fieldInfo);

        Assert.True(descriptor1 == descriptor2);
    }

    [Fact]
    public void OperatorEquals_WithDifferentFieldInfo_ReturnsFalse()
    {
        var fieldInfo1 = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var fieldInfo2 = typeof(TestClass).GetField(nameof(TestClass.StringField))!;
        var descriptor1 = new FieldPropertyDescriptor(fieldInfo1);
        var descriptor2 = new FieldPropertyDescriptor(fieldInfo2);

        Assert.False(descriptor1 == descriptor2);
    }

    [Fact]
    public void OperatorEquals_WithNulls_HandlesCorrectly()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.True((FieldPropertyDescriptor?)null == (FieldPropertyDescriptor?)null);
        Assert.False(descriptor == null);
        Assert.False(null == descriptor);
    }

    [Fact]
    public void OperatorNotEquals_WithSameFieldInfo_ReturnsFalse()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor1 = new FieldPropertyDescriptor(fieldInfo);
        var descriptor2 = new FieldPropertyDescriptor(fieldInfo);

        Assert.False(descriptor1 != descriptor2);
    }

    [Fact]
    public void OperatorNotEquals_WithDifferentFieldInfo_ReturnsTrue()
    {
        var fieldInfo1 = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var fieldInfo2 = typeof(TestClass).GetField(nameof(TestClass.StringField))!;
        var descriptor1 = new FieldPropertyDescriptor(fieldInfo1);
        var descriptor2 = new FieldPropertyDescriptor(fieldInfo2);

        Assert.True(descriptor1 != descriptor2);
    }

    [Fact]
    public void OperatorNotEquals_WithNulls_HandlesCorrectly()
    {
        var fieldInfo = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;
        var descriptor = new FieldPropertyDescriptor(fieldInfo);

        Assert.False((FieldPropertyDescriptor?)null != (FieldPropertyDescriptor?)null);
        Assert.True(descriptor != null);
        Assert.True(null != descriptor);
    }
}
