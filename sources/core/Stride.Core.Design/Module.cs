// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.TypeConverters;

namespace Stride.Core
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
            TypeDescriptor.AddAttributes(typeof(IUrlReference), new TypeConverterAttribute(typeof(UrlReferenceConverter)));
        }
    }
}
