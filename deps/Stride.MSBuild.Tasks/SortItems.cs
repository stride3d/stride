// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Stride.MSBuild.Tasks
{
    public class SortItems : Task
    {
        /// <summary>
        /// Gets the InputItems.
        /// </summary>
        [Required]
        public ITaskItem[] InputItems { get; set; }

        /// <summary>
        /// Gets the OutputItems.
        /// </summary>
        [Output]
        public ITaskItem[] OutputItems { get; set; }

        public override bool Execute()
        {
            if (InputItems == null)
            {
                Log.LogError("InputItems is not set");
                return false;
            }

            OutputItems = InputItems.OrderBy(x => x.ItemSpec).ToArray();

            return true;
        }
    }
}
