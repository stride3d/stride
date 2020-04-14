// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;

namespace Stride.Particles.Spawners
{
    /// <summary>
    /// Some initializers require fine control between parent and child emitters. Use the control group to assign such meta-fields.
    /// </summary>
    /// <userdoc>
    /// Some initializers require fine control between parent and child emitters. Use the control group to assign such meta-fields.
    /// </userdoc>
    [DataContract("ParentControlFlag")]
    [Display("Control group")]
    public enum ParentControlFlag
    {
        /// <summary>
        /// Use a meta-data control group with index 0
        /// </summary>
        [Display("Group 0")]
        Group00 = 0,

        /// <summary>
        /// Use a meta-data control group with index 1
        /// </summary>
        [Display("Group 1")]
        Group01 = 1,

        /// <summary>
        /// Use a meta-data control group with index 2
        /// </summary>
        [Display("Group 2")]
        Group02 = 2,

        /// <summary>
        /// Use a meta-data control group with index 3
        /// </summary>
        [Display("Group 3")]
        Group03 = 3,




        /// <summary>
        /// Do not use meta-data control groups
        /// </summary>
        [Display("None")]
        None = int.MaxValue
    }
}
