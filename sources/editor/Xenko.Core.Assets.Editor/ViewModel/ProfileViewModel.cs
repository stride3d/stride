// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;

using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    // TODO: Check if we can turn this into a SessionObjectViewModel
    public class ProfileViewModel : DispatcherViewModel
    {
        private readonly PackageProfile profile;
        private readonly SessionViewModel session;
        private Package package;

        public ProfileViewModel(SessionViewModel session, Package package, PackageProfile profile, PackageViewModel container)
            : base(session.ServiceProvider)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            this.session = session;
            this.package = package;
            this.profile = profile;

            Package = container;
        }

        public PackageViewModel Package { get; }
    }
}
