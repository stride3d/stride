using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

#pragma warning disable

namespace CppNet
{
    internal class JavaFile : VirtualFile
    {
        string _path;

        public JavaFile(string path)
        {
            _path = Path.GetFullPath(path);
        }

        public bool isFile()
        {
            return File.Exists(_path) && !File.GetAttributes(_path).HasFlag(FileAttributes.Directory);
        }

        public string getPath()
        {
            return _path;
        }

        public string getName()
        {
            return Path.GetFileName(_path);
        }

        public VirtualFile getParentFile()
        {
            return new JavaFile(Path.GetDirectoryName(_path));
        }

        public VirtualFile getChildFile(string name)
        {
            return new JavaFile(Path.Combine(_path, name));
        }

        public Source getSource()
        {
            return new FileLexerSource(_path);
        }
    }
}
