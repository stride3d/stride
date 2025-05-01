// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

internal sealed class SessionNodeContainer : AssetNodeContainer
{
    private readonly IReadOnlyList<Type> additionalPrimitiveTypes;

    public SessionNodeContainer(IViewModelServiceProvider serviceProvider)
    {
        // Apply primitive types
        var pluginService = serviceProvider.Get<IAssetsPluginService>();
        additionalPrimitiveTypes = pluginService.GetPrimitiveTypes();
    }

    public override bool IsPrimitiveType(Type type)
    {
        return base.IsPrimitiveType(type) || additionalPrimitiveTypes.Any(x => x.IsAssignableFrom(type));
    }
}
