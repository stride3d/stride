// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Globalization;
using Xenko.Core.Mathematics;
using Xenko.Core.Reflection;
using Xenko.Core.TypeConverters;

namespace Xenko.Core
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Make sure that this assembly is registered
            AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
            TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(ColorConverter)));
            TypeDescriptor.AddAttributes(typeof(Color3), new TypeConverterAttribute(typeof(Color3Converter)));
            TypeDescriptor.AddAttributes(typeof(Color4), new TypeConverterAttribute(typeof(Color4Converter)));
            TypeDescriptor.AddAttributes(typeof(Half), new TypeConverterAttribute(typeof(HalfConverter)));
            TypeDescriptor.AddAttributes(typeof(Half2), new TypeConverterAttribute(typeof(Half2Converter)));
            TypeDescriptor.AddAttributes(typeof(Half3), new TypeConverterAttribute(typeof(Half3Converter)));
            TypeDescriptor.AddAttributes(typeof(Half4), new TypeConverterAttribute(typeof(Half4Converter)));
            TypeDescriptor.AddAttributes(typeof(Matrix), new TypeConverterAttribute(typeof(MatrixConverter)));
            TypeDescriptor.AddAttributes(typeof(Quaternion), new TypeConverterAttribute(typeof(QuaternionConverter)));
            TypeDescriptor.AddAttributes(typeof(Vector2), new TypeConverterAttribute(typeof(Vector2Converter)));
            TypeDescriptor.AddAttributes(typeof(Vector3), new TypeConverterAttribute(typeof(Vector3Converter)));
            TypeDescriptor.AddAttributes(typeof(Vector4), new TypeConverterAttribute(typeof(Vector4Converter)));

            TypeDescriptor.AddAttributes(typeof(Xenko.Core.Serialization.UrlReference), new TypeConverterAttribute(typeof(UrlReferenceConverter)));
        }
    }

    //TODO: Move this to the correct place.
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
            return Serialization.UrlReferenceHelper.IsUrlReferenceType(TypeConverterHelper.GetDestinationType(context));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var attachedReference = Serialization.AttachedReferenceManager.GetAttachedReference(value);
            var destinationType = TypeConverterHelper.GetDestinationType(context);
            return Serialization.AttachedReferenceManager.CreateProxyObject(destinationType, attachedReference.Id, attachedReference.Url);
        }
    }
}
