// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Globalization;
using Xenko.Core.Serialization;

namespace Xenko.Core.TypeConverters
{
    /// <summary>
    /// Defines a type converter for <see cref="UrlReference"/>.
    /// </summary>
    public class UrlReferenceConverter : BaseConverter
    {

        public UrlReferenceConverter()
        {

            //var type = typeof(Xenko.Core.Serialization.UrlReference);
            //Properties = new PropertyDescriptorCollection(new System.ComponentModel.PropertyDescriptor[]
            //{
            //    new Reflection.PropertyDescriptor(type.GetProperty(nameof(Xenko.Core.Serialization.UrlReference.Url))),
            //});
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return UrlReferenceHelper.IsUrlReferenceType(TypeConverterHelper.GetDestinationType(context));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var attachedReference = AttachedReferenceManager.GetAttachedReference(value);
            var destinationType = TypeConverterHelper.GetDestinationType(context);
            return UrlReferenceHelper.CreateReference(destinationType, attachedReference.Id, attachedReference.Url);
        }
    }
}
