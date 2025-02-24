// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Stride.Core.TypeConverters;

public sealed class FieldPropertyDescriptor : PropertyDescriptor, IEquatable<FieldPropertyDescriptor>
{
    public FieldInfo FieldInfo { get; }

    public override Type ComponentType => FieldInfo.DeclaringType!;

    public override bool IsReadOnly => false;

    public override Type PropertyType => FieldInfo.FieldType;

    public FieldPropertyDescriptor(FieldInfo fieldInfo)
        : base(fieldInfo.Name, [])
    {
        FieldInfo = fieldInfo;

        var attributesObject = fieldInfo.GetCustomAttributes(true);
        var attributes = new Attribute[attributesObject.Length];
        for (int i = 0; i < attributes.Length; i++)
            attributes[i] = (Attribute)attributesObject[i];
        AttributeArray = attributes;
    }

    public override bool CanResetValue(object component)
    {
        return false;
    }

    public override object? GetValue(object? component)
    {
        return FieldInfo.GetValue(component);
    }

    public override void ResetValue(object component)
    {
    }

    public override void SetValue(object? component, object? value)
    {
        FieldInfo.SetValue(component, value);
        OnValueChanged(component, EventArgs.Empty);
    }

    public override bool ShouldSerializeValue(object component)
    {
        return true;
    }

    /// <inheritdoc />
    public bool Equals([NotNullWhen(true)] FieldPropertyDescriptor? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(FieldInfo, other.FieldInfo);
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is FieldPropertyDescriptor descriptor && Equals(descriptor);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return FieldInfo.GetHashCode();
    }

    public static bool operator ==(FieldPropertyDescriptor? left, FieldPropertyDescriptor? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(FieldPropertyDescriptor? left, FieldPropertyDescriptor? right)
    {
        return !Equals(left, right);
    }
}
