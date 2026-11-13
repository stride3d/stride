// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using Stride.Assets.Editor.Templates;
using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Serialization;
using Stride.Rendering;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Assets.Presentation.Templates
{
    /// <summary>
    /// The options of a model import (<see cref="ImportModelFromFileViewModel"/>). The skeleton to reuse is picked with the content
    /// reference template of the property grid, through a property graph of its own (<see cref="ReferenceViewModel"/>); it is
    /// copied to the options when the user accepts (<see cref="ApplySkeleton"/>).
    /// </summary>
    public partial class ModelAssetTemplateWindow
    {
        /// <summary>
        /// The object holding the skeleton reference that the property template edits.
        /// </summary>
        [DataContract]
        public sealed class DummyReferenceContainer : IPropertyProviderViewModel
        {
            private readonly IObjectNode rootNode;

            public DummyReferenceContainer(Skeleton skeleton)
            {
                Skeleton = skeleton;
                rootNode = SessionViewModel.Instance.AssetNodeContainer.GetOrCreateNode(this);
            }

            public Skeleton Skeleton { get; set; }

            bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

            IObjectNode IPropertyProviderViewModel.GetRootNode() => rootNode;

            bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

            bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
        }

        private readonly DummyReferenceContainer referenceContainer;

        public ModelAssetTemplateWindow(ImportModelFromFileViewModel parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            var session = SessionViewModel.Instance;
            var skeleton = parameters.SkeletonToReuse is { } asset ? ContentReferenceHelper.CreateReference<Skeleton>(asset) : null;
            referenceContainer = new DummyReferenceContainer(skeleton);
            var viewModelService = new GraphViewModelService(session.AssetNodeContainer);
            var services = new ViewModelServiceProvider(session.ServiceProvider, viewModelService.Yield());
            ReferenceViewModel = GraphViewModel.Create(services, new[] { referenceContainer });
            Parameters = parameters;
            InitializeComponent();
        }

        public ImportModelFromFileViewModel Parameters { get { return (ImportModelFromFileViewModel)DataContext; } private set { DataContext = value; } }

        /// <summary>
        /// The property graph of the skeleton reference, shown by the content reference template.
        /// </summary>
        public GraphViewModel ReferenceViewModel { get; }

        /// <summary>
        /// Copies the skeleton picked in the window to <see cref="ImportModelFromFileViewModel.SkeletonToReuse"/>.
        /// </summary>
        internal void ApplySkeleton()
        {
            var id = AttachedReferenceManager.GetAttachedReference(referenceContainer.Skeleton)?.Id;
            Parameters.SkeletonToReuse = id is { } skeletonId ? SessionViewModel.Instance.GetAssetById(skeletonId) : null;
        }

        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            Result = Stride.Core.Presentation.Services.DialogResult.Ok;
            Close();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            Result = Stride.Core.Presentation.Services.DialogResult.Cancel;
            Close();
        }
    }
}
