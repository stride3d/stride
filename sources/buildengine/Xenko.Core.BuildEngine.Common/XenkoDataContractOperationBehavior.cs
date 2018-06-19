// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml;

namespace Xenko.Core.BuildEngine
{
    public class XenkoDataContractOperationBehavior : DataContractSerializerOperationBehavior
    {
        private static XenkoXmlObjectSerializer serializer = new XenkoXmlObjectSerializer();

        public XenkoDataContractOperationBehavior(OperationDescription operation)
            : base(operation)
        {
        }

        public XenkoDataContractOperationBehavior(
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
