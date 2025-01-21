// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;

namespace Stride.Core;

public interface IService
{
    [NotNull] public static abstract IService NewInstance([NotNull] IServiceRegistry services);
}
