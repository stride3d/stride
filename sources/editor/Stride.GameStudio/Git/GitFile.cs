using LibGit2Sharp;

namespace Stride.GameStudio.Git
{
    public class GitFile
    {
        public string RelativeFilePath { get; set; }
        public FileStatus Status { get; set; }
        public bool IsStaged { get; set; }
    }
}
