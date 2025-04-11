// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using Stride.Core.Serialization;

namespace Stride.Core.TypeConverters;

/// <summary>
/// Defines a type converter for <see cref="IUrlReference"/>.
/// </summary>
public class UrlReferenceConverter : BaseConverter
{
    public UrlReferenceConverter()
    {
        //TODO: PropertyDescriptor does not support Properties, only fields so can not currently Set Properties. Does not seem to impact usage.
    }

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return UrlReferenceBase.IsUrlReferenceType(TypeConverterHelper.GetDestinationType(context));
    }

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (AttachedReferenceManager.GetAttachedReference(value) is {} attachedReference && TypeConverterHelper.GetDestinationType(context) is {} destinationType)
        {
            return UrlReferenceBase.New(destinationType, attachedReference.Id, attachedReference.Url);
        }
        return null;
    }
}
