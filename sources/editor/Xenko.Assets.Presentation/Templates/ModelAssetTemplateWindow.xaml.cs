// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.Templates
{
    public class ImportModelFromFileViewModel : DispatcherViewModel
    {
        [DataContract]
        public class DummyReferenceContainer : IPropertyProviderViewModel
        {
            private readonly IObjectNode rootNode;

            public DummyReferenceContainer()
            {
                rootNode = SessionViewModel.Instance.AssetNodeContainer.GetOrCreateNode(this);
            }

            public Skeleton Skeleton { get; set; }

            bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

            IObjectNode IPropertyProviderViewModel.GetRootNode() => rootNode;

            bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

            bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
        }

        private readonly DummyReferenceContainer referenceContainer = new DummyReferenceContainer();

        private bool importMaterials = true;
        private bool importTextures = true;
        private bool importSkeleton = true;
        private bool dontImportSkeleton;
        private bool reuseSkeleton;

        public ImportModelFromFileViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            ReferenceViewModel = GraphViewModel.Create(serviceProvider, new[] { referenceContainer });
        }

        public bool ImportMaterials { get { return importMaterials; } set { SetValue(ref importMaterials, value); } }

        public bool ImportTextures { get { return importTextures; } set { SetValue(ref importTextures, value); } }

        public bool ImportSkeleton { get { return importSkeleton; } set { SetValue(ref importSkeleton, value); } }

        public bool DontImportSkeleton { get { return dontImportSkeleton; } set { SetValue(ref dontImportSkeleton, value); } }

        public bool ReuseSkeleton { get { return reuseSkeleton; } set { SetValue(ref reuseSkeleton, value); } }

        public Skeleton SkeletonToReuse { get { return referenceContainer.Skeleton; } set { referenceContainer.Skeleton = value; } }

        public GraphViewModel ReferenceViewModel { get; }
    }
    /// <summary>
    /// Interaction logic for ModelAssetTemplateWindow.xaml
    /// </summary>
    public partial class ModelAssetTemplateWindow
    {
        public ModelAssetTemplateWindow()
        {
            var viewModelService = new GraphViewModelService(SessionViewModel.Instance.AssetNodeContainer);
            var services = new ViewModelServiceProvider(SessionViewModel.Instance.ServiceProvider, viewModelService.Yield());
            Parameters = new ImportModelFromFileViewModel(services);
            InitializeComponent();
        }

        public ImportModelFromFileViewModel Parameters { get { return (ImportModelFromFileViewModel)DataContext; } set { DataContext = value; } }

        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            Result = Xenko.Core.Presentation.Services.DialogResult.Ok;
            Close();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            Result = Xenko.Core.Presentation.Services.DialogResult.Cancel;
            Close();
        }
    }
}
