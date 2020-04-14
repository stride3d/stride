// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Windows;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Presentation.Behaviors;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.ScriptEditor
{
    public class BindScriptTextEditorWorkspaceProjectIdBehavior : DeferredBehaviorBase<ScriptTextEditor>
    {
        public static readonly DependencyProperty PackageProperty = DependencyProperty.Register("Package", typeof(PackageViewModel), typeof(BindScriptTextEditorWorkspaceProjectIdBehavior));

        public PackageViewModel Package { get { return (PackageViewModel)GetValue(PackageProperty); } set { SetValue(PackageProperty, value); } }

        protected override async void OnAttachedAndLoaded()
        {
            base.OnAttachedAndLoaded();

            var package = Package;
            if (package == null)
                return;

            // Wait for project watcher to be ready
            var strideAssets = await StrideAssetsViewModel.InstanceTask;
            var code = strideAssets.Code;

            var projectWatcher = await code.ProjectWatcher;

            // Find roslyn project
            var projectFullPath = (package.Package.Container as SolutionProject)?.FullPath;
            var roslynProject = projectFullPath != null ? projectWatcher.TrackedAssemblies.FirstOrDefault(x => new UFile(x.Project.FilePath) == projectFullPath)?.Project : null;
            if (roslynProject == null)
                return;

            AssociatedObject.Workspace = await code.Workspace;
            AssociatedObject.ProjectId = roslynProject.Id;
        }

        protected override void OnDetachingAndUnloaded()
        {
            AssociatedObject.ProjectId = null;
            AssociatedObject.Workspace = null;
            base.OnDetachingAndUnloaded();
        }
    }
}
