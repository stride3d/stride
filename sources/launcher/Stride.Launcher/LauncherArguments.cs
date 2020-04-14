// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.LauncherApp
{
    /// <summary>
    /// A structure representing the arguments passed to the launcher process.
    /// </summary>
    internal struct LauncherArguments
    {
        /// <summary>
        /// An enum representing the type of action this process should perform.
        /// </summary>
        public enum ActionType
        {
            Run,
            Uninstall,
        }

        /// <summary>
        /// The list of actions this process should perform.
        /// </summary>
        public List<ActionType> Actions;
    }
}
