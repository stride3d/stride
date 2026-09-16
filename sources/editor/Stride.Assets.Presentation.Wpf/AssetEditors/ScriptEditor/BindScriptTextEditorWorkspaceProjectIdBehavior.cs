// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Windows;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
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

            // Wait for the workspace to be ready
            var strideAssets = await StrideAssetsViewModel.InstanceTask;
            var code = strideAssets.Code;

            var projectFullPath = (package.Package.Container as SolutionProject)?.FullPath;
            if (projectFullPath == null)
                return;

            // Resolve the project id from the workspace itself: its id is stable across reloads,
            // whereas the MSBuild snapshot id churns on every solution reload.
            var workspace = await code.Workspace;
            var osPath = projectFullPath.ToOSPath();
            var roslynProject = workspace.CurrentSolution.Projects.FirstOrDefault(x => string.Equals(x.FilePath, osPath, StringComparison.OrdinalIgnoreCase));
            if (roslynProject == null)
                return;

            AssociatedObject.Workspace = workspace;
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
