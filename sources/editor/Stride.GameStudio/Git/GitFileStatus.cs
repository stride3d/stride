using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.GameStudio.Git
{
    /// <summary>
    /// Represents the status of a file in a Git repository.
    /// </summary>
    public enum GitFileStatus
    {
        Added,
        Deleted,
        Modified,
        Untracked
    }
}
