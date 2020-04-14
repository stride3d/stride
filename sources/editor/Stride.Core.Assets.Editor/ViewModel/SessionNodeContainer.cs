// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.Quantum;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Extensions;

namespace Xenko.Core.Assets.Editor.ViewModel
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
