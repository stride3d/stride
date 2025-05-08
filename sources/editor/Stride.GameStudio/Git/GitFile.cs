using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.GameStudio.Git
{
    public class GitFile
    {
        public string RelativeFilePath { get; set; }
        public GitFileStatus Status { get; set; }
        public bool IsStaged { get; set; }
    }
}
