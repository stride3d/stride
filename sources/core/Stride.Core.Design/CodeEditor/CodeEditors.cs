// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.VisualStudio;

namespace Stride.Core.CodeEditor
{
    public static class CodeEditors
    {
        private static readonly Lazy<List<CodeEditor>> CodeEditorsList = new Lazy<List<CodeEditor>>(CollectCodeEditors);
        
        public static IEnumerable<CodeEditor> AvailableCodeEditors => CodeEditorsList.Value;
        
        public static CodeEditor DefaultCodeEditor = new CodeEditor(new Version("0.0"), "Default Code Editor", string.Empty);

        private static List<CodeEditor> CollectCodeEditors()
        {
            var ides = new List<CodeEditor>();
            
            ides.Add(DefaultCodeEditor);

            ides.AddRange(RiderPathLocator.GetAllRiderPaths().Select(a => new CodeEditor(a.BuildNumber, a.Presentation, a.Path)));
            ides.AddRange(VisualStudioVersions.AvailableVisualStudioInstances.Select(a => new CodeEditor(a.Version, a.DisplayName, a.DevenvPath)));

            return ides;
        }
    }
}
