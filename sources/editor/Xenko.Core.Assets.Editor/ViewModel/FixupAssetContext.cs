// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.Assets.Editor.ViewModel
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
