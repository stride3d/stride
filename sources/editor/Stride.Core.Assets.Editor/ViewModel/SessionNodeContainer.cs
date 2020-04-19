// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public class SessionNodeContainer : AssetNodeContainer
    {
        public SessionNodeContainer(SessionViewModel session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            // Apply primitive types, commands and associated data providers that comes from plugins
            var pluginService = session.ServiceProvider.Get<IAssetsPluginService>();
            pluginService.GetPrimitiveTypes(session).ForEach(x => NodeBuilder.RegisterPrimitiveType(x));
        }
    }
}
