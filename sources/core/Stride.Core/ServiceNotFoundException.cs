// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core;

public class ServiceNotFoundException : Exception
{
    public ServiceNotFoundException()
    {
    }

    public ServiceNotFoundException(Type serviceType)
        : base(FormatServiceNotFoundMessage(serviceType))
    {
        ServiceType = serviceType;
    }

    public ServiceNotFoundException(Type serviceType, Exception innerException)
        : base(FormatServiceNotFoundMessage(serviceType), innerException)
    {
        ServiceType = serviceType;
    }

    public Type ServiceType { get; }

    private static string FormatServiceNotFoundMessage(Type serviceType)
    {
        return $"Service [{serviceType.Name}] not found";
    }
}
