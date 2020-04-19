// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Components.Properties
{
    public class UPathReferenceViewModel : AddReferenceViewModel
    {
        public override bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            UPath path = null;
            var singleChild = true;
            foreach (var child in children)
            {
                if (!singleChild)
                {
                    message = "Multiple files selected";
                    return false;
                }
                path = child as UPath;
                if (path == null)
                {
                    message = "The selection is not a file nor a directory";
                    return false;
                }
                singleChild = false;
            }
            if (path == null)
            {
                message = "The selection is not a file nor a directory";
                return false;
            }
            message = $"Use {path}";
            return true;
        }

        public override void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            var path = (UFile)children.First();
            var dirPath = new UDirectory(path);
            if (TargetNode.Type == typeof(UFile) && File.Exists(path))
            {
                TargetNode.NodeValue = path;
            }
            if (TargetNode.Type == typeof(UDirectory) && Directory.Exists(dirPath))
            {
                TargetNode.NodeValue = dirPath;
            }
        }
    }
}
