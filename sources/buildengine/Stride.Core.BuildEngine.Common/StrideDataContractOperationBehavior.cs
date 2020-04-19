// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml;

namespace Stride.Core.BuildEngine
{
    public class StrideDataContractOperationBehavior : DataContractSerializerOperationBehavior
    {
        private static StrideXmlObjectSerializer serializer = new StrideXmlObjectSerializer();

        public StrideDataContractOperationBehavior(OperationDescription operation)
            : base(operation)
        {
        }

        public StrideDataContractOperationBehavior(
            OperationDescription operation,
            DataContractFormatAttribute dataContractFormatAttribute)
            : base(operation, dataContractFormatAttribute)
        {
        }

        public override XmlObjectSerializer CreateSerializer(
            Type type, string name, string ns, IList<Type> knownTypes)
        {
            return serializer;
        }

        public override XmlObjectSerializer CreateSerializer(
            Type type, XmlDictionaryString name, XmlDictionaryString ns,
            IList<Type> knownTypes)
        {
            return serializer;
        }
    }
}
