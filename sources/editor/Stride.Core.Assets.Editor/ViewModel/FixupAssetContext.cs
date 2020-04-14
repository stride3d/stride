// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets.Editor.ViewModel
{
    internal class FixupAssetContext : IDisposable
    {
        private readonly SessionViewModel session;

        public FixupAssetContext(SessionViewModel session)
        {
            this.session = session;
            session.IsInFixupAssetContext = true;
        }

        public void Dispose()
        {
            session.IsInFixupAssetContext = false;
        }
    }
}
