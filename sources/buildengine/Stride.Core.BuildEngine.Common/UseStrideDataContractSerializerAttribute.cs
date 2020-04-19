// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Stride.Core.BuildEngine
{
    public class UseStrideDataContractSerializerAttribute : Attribute, IOperationBehavior
    {
        public void AddBindingParameters(OperationDescription description,
                                         BindingParameterCollection parameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription description,
                                        System.ServiceModel.Dispatcher.ClientOperation proxy)
        {
            ReplaceDataContractSerializerOperationBehavior(description);
        }

        public void ApplyDispatchBehavior(OperationDescription description,
                                          System.ServiceModel.Dispatcher.DispatchOperation dispatch)
        {
            ReplaceDataContractSerializerOperationBehavior(description);
        }

        public void Validate(OperationDescription description)
        {
        }

        private static void ReplaceDataContractSerializerOperationBehavior(
            OperationDescription description)
        {
            var dcsOperationBehavior =
                description.Behaviors.Find<DataContractSerializerOperationBehavior>();

            if (dcsOperationBehavior != null)
            {
                description.Behaviors.Remove(dcsOperationBehavior);
                description.Behaviors.Add(new StrideDataContractOperationBehavior(description));
            }
        }
    }
}
