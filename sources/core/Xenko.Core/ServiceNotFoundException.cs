// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;

namespace Xenko.Core
{
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException()
        {
        }

        public ServiceNotFoundException([NotNull] Type serviceType)
            : base(FormatServiceNotFoundMessage(serviceType))
        {
            ServiceType = serviceType;
        }

        public ServiceNotFoundException([NotNull] Type serviceType, Exception innerException)
            : base(FormatServiceNotFoundMessage(serviceType), innerException)
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; private set; }

        [NotNull]
        private static string FormatServiceNotFoundMessage([NotNull] Type serviceType)
        {
            return $"Service [{serviceType.Name}] not found";
        }
    }
}
