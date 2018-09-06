// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Windows;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Behaviors;
using Xenko.Assets.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.AssetEditors.ScriptEditor
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
            var xenkoAssets = await XenkoAssetsViewModel.InstanceTask;
            var code = xenkoAssets.Code;

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
