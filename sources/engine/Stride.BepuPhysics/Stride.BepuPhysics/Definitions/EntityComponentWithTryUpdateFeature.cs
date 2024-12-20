﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Engine;

namespace Stride.BepuPhysics.Definitions
{
    public abstract class EntityComponentWithTryUpdateFeature : StartupScript
    {
        internal abstract void TryUpdateFeatures();
    }
}
