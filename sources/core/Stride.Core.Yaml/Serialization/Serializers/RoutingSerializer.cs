// Copyright (c) 2015 SharpYaml - Alexandre Mutel
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
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

namespace Stride.Core.Yaml.Serialization.Serializers
{
    /// <summary>
    /// This serializer is responsible to route to a specific serializer.
    /// </summary>
    public class RoutingSerializer : ChainedSerializer, IYamlSerializable
    {
        private readonly ISerializerFactorySelector serializerSelector;

        public RoutingSerializer(ISerializerFactorySelector serializerSelector)
        {
            if (serializerSelector == null) throw new ArgumentNullException(nameof(serializerSelector));
            this.serializerSelector = serializerSelector;
        }

        public sealed override object ReadYaml(ref ObjectContext objectContext)
        {
            // If value is not null, use its TypeDescriptor otherwise use expected type descriptor
            var instance = objectContext.Instance;
            var typeDescriptorOfValue = instance != null ? objectContext.SerializerContext.FindTypeDescriptor(instance.GetType()) : objectContext.Descriptor;

            var serializer = serializerSelector.GetSerializer(objectContext.SerializerContext, typeDescriptorOfValue);
            return serializer.ReadYaml(ref objectContext);
        }

        public sealed override void WriteYaml(ref ObjectContext objectContext)
        {
            var serializer = serializerSelector.GetSerializer(objectContext.SerializerContext, objectContext.Descriptor);
            serializer.WriteYaml(ref objectContext);
        }
    }
}
