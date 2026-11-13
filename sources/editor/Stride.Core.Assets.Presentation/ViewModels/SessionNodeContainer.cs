// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System;
using Stride.Core.Assets.Presentation.Services;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Presentation.ViewModels
{
    public class SessionNodeContainer : AssetNodeContainer
    {
        private readonly Type[] additionalPrimitiveTypes;
        
        public SessionNodeContainer(ISessionViewModel session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            // Apply primitive types, commands and associated data providers that comes from plugins
            var pluginService = session.ServiceProvider.Get<IPresentationPluginService>();
            additionalPrimitiveTypes = pluginService.GetPrimitiveTypes(session).ToArray();
        }

        public override bool IsPrimitiveType(Type type)
        {
            return base.IsPrimitiveType(type) || additionalPrimitiveTypes.Any(x => x.IsAssignableFrom(type));
        }
    }
}
