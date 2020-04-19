// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Quantum;
using Stride.Assets.Entities;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(SceneAsset))]
    public sealed class SceneViewModel : EntityHierarchyViewModel, IAssetViewModel<SceneAsset>
    {
        private readonly IObjectNode childrenNode;
        private readonly IMemberNode parentNode;
        private SceneViewModel parent;

        private bool isInitialized;
        private bool updatingChildren;
        private bool updatingParent;

        public SceneViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
            SetAsDefaultCommand = new AnonymousCommand(ServiceProvider, SetAsDefault);
            assetCommands.Add(new MenuCommandInfo(ServiceProvider, SetAsDefaultCommand) { DisplayName = "Set as default", Tooltip = "Set as default scene" });
            UpdateCommands();

            Session.SessionStateChanged += SessionStateChanged;

            var assetNode = NodeContainer.GetNode(Asset);
            childrenNode = assetNode[nameof(SceneAsset.ChildrenIds)].Target;
            parentNode = assetNode[nameof(SceneAsset.Parent)];
        }

        /// <inheritdoc />
        public new SceneAsset Asset => (SceneAsset)base.Asset;

        [ItemNotNull, NotNull]
        public IObservableList<SceneViewModel> Children { get; } = new ObservableSet<SceneViewModel>();

        [CanBeNull]
        public SceneViewModel Parent { get { return parent; } private set { SetValueUncancellable(ref parent, value, ParentPropertyChanged); } }

        [NotNull]
        internal ICommandBase SetAsDefaultCommand { get; }

        /// <summary>
        /// Checks whether this scene can be a parent of the provided <paramref name="child"/> scene.
        /// </summary>
        /// <param name="child">The potential child of this scene.</param>
        /// <param name="message">A message providing more details on why this scene can -or cannot- be a parent.</param>
        /// <param name="checkSameParent">
        /// <c>true</c> to also check if this scene is already a parent of the provided <paramref name="child"/> scene; otherwise, <c>false</c>.
        /// The default is <c>false</c>.
        /// </param>
        /// <returns><c>true</c> if this scene can be of parent of the provided <paramref name="child"/> scene; otherwise, <c>false</c>.</returns>
        public bool CanBeParentOf([NotNull] SceneViewModel child, [NotNull] out string message, bool checkSameParent = false)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));

            if (child == this)
            {
                message = "Can't be a parent of itself";
                return false;
            }
            if (checkSameParent && child.Parent == this)
            {
                message = "Scene already has this parent";
                return false;
            }
            var scene = Parent;
            while (scene != null)
            {
                if (scene == child)
                {
                    message = "Circular scene hierarchy";
                    return false;
                }
                scene = scene.Parent;
            }
            message = "Can be a parent of this scene";
            return true;
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SceneViewModel));

            parentNode.ValueChanged -= ParentNodeValueChanged;
            childrenNode.ItemChanged -= ChildrenNodeItemChanged;
            Children.CollectionChanged -= ChildrenCollectionChanged;
            Session.SessionStateChanged -= SessionStateChanged;

            base.Destroy();
        }

        /// <summary>
        /// Returns the root of this <see cref="SceneViewModel"/>, or itself if it has no parent.
        /// </summary>
        /// <returns>The root scene, or itself if it has no parent.</returns>
        [NotNull]
        public SceneViewModel GetRoot()
        {
            var root = this;
            var hash = new HashSet<AssetId> { root.Id };
            while (root.Parent != null)
            {
                if (!hash.Add(root.Parent.Id))
                {
                    GlobalLogger.GetLogger("Asset").Error($"Cyclic reference detected for scene {root.Name} (Id={root.Id}).");
                    break;
                }
                root = root.Parent;
            }
            return root;
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            var invalidIndices = new Stack<int>();
            for (var i = 0; i < Asset.ChildrenIds.Count; i++)
            {
                var childId = Asset.ChildrenIds[i];
                var childScene = Session.GetAssetById(childId) as SceneViewModel;
                if (childScene == null)
                {
                    // Mark this id for deletion
                    invalidIndices.Push(i);
                    continue;
                }

                // We trust the child's Parent property. Let's check the relationship or fix it.
                var parentValue = childScene.parentNode.Retrieve();
                var parentReference = AttachedReferenceManager.GetAttachedReference(parentValue);
                if (parentReference?.Id != Id)
                {
                    // Mark this id for deletion
                    invalidIndices.Push(i);
                    // If the parent is already initialized and the child is connected, there is nothing to do
                    if (childScene.Parent?.isInitialized == true)
                    {
                        Debug.Assert(childScene.Parent.Children.Contains(childScene));
                    }
                    else
                    {
                        // Otherwise: fixup the parent child relationship
                        FixupParentChild(childScene, parentReference);
                    }
                }
                else
                {
                    // Everything good
                    childScene.parent = this;
                    Children.Add(childScene);
                }
            }
            foreach (var index in invalidIndices)
            {
                childrenNode.Remove(Asset.ChildrenIds[index], new NodeIndex(index));
            }

            if (Parent?.isInitialized == true)
            {
                Debug.Assert(Parent.Children.Contains(this));
            }
            else
            {
                var parentValue = parentNode.Retrieve();
                var parentReference = AttachedReferenceManager.GetAttachedReference(parentValue);
                FixupParentChild(this, parentReference);
            }

            Children.CollectionChanged += ChildrenCollectionChanged;
            childrenNode.ItemChanged += ChildrenNodeItemChanged;
            parentNode.ValueChanged += ParentNodeValueChanged;
            isInitialized = true;
        }

        private static void FixupParentChild([NotNull] SceneViewModel childScene, AttachedReference parentReference)
        {
            if (parentReference == null)
            {
                // If this child does not have a parent, do nothing
                return;
            }

            var parent = childScene.Session.GetAssetById(parentReference.Id) as SceneViewModel;
            if (parent == null)
            {
                // Not a scene, let's clean the reference
                childScene.Parent = null;
                return;
            }

            if (!parent.isInitialized)
            {
                if (!parent.Asset.ChildrenIds.Contains(childScene.Id))
                {
                    // The parent doesn't know about this child, let's fix it
                    parent.childrenNode.Add(childScene.Id);
                }
                // child's Parent property will be fixed up during its parent initialization, nothing more to do
                return;
            }

            // The parent is already initialized but doesn't know about this child, let's add it
            childScene.parent = parent;
            parent.Children.Add(childScene);
        }

        /// <inheritdoc />
        protected override void UpdateIsDeletedStatus()
        {
            base.UpdateIsDeletedStatus();
            if (IsDeleted)
            {
                Parent?.Children.Remove(this);
            }
        }

        private void ChildrenCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            if (updatingChildren || UndoRedoService.UndoRedoInProgress)
                return;

            try
            {
                updatingChildren = true;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems?.Count > 0)
                        {
                            foreach (var scene in e.OldItems.Cast<SceneViewModel>())
                            {
                                scene.Parent = null;
                                childrenNode.Remove(scene.Id, new NodeIndex(e.OldStartingIndex));
                            }
                        }
                        if (e.NewItems?.Count > 0)
                        {
                            var index = e.NewStartingIndex;
                            foreach (var scene in e.NewItems.Cast<SceneViewModel>())
                            {
                                // Note: this can happen after a cut/paste when the parent/child relationship is fixed-up.
                                if (scene.Parent != this)
                                {
                                    scene.Parent?.Children.Remove(scene);
                                    scene.Parent = this;
                                }
                                childrenNode.Add(scene.Id, new NodeIndex(index++));
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Reset:
                        throw new NotSupportedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                updatingChildren = false;
            }
        }

        private void ChildrenNodeItemChanged(object sender, ItemChangeEventArgs e)
        {
            if (updatingChildren)
                return;

            try
            {
                updatingChildren = true;
                switch (e.ChangeType)
                {
                    case ContentChangeType.CollectionUpdate:
                    case ContentChangeType.CollectionAdd:
                    case ContentChangeType.CollectionRemove:
                        AssetId childId;
                        SceneViewModel childScene;
                        if (e.OldValue != null)
                        {
                            childId = (AssetId)e.OldValue;
                            childScene = (SceneViewModel)Session.GetAssetById(childId);
                            childScene.Parent = null;
                            Children.Remove(childScene);
                        }
                        if (e.NewValue != null)
                        {
                            childId = (AssetId)e.NewValue;
                            childScene = (SceneViewModel)Session.GetAssetById(childId);
                            childScene.Parent?.Children.Remove(childScene);
                            childScene.Parent = this;
                            Children.Insert(e.Index.Int, childScene);
                        }
                        break;

                    case ContentChangeType.None:
                    case ContentChangeType.ValueChange:
                        throw new InvalidOperationException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                updatingChildren = false;
            }
        }

        private void ParentNodeValueChanged(object sender, MemberNodeChangeEventArgs e)
        {
            if (updatingParent)
                return;

            try
            {
                updatingParent = true;

                var oldParentReference = AttachedReferenceManager.GetAttachedReference(e.OldValue);
                if (oldParentReference != null)
                {
                    var oldParent = (SceneViewModel)Session.GetAssetById(oldParentReference.Id);
                    oldParent?.Children.Remove(this);
                }

                var newParentReference = AttachedReferenceManager.GetAttachedReference(e.NewValue);
                if (newParentReference != null)
                {
                    var newParent = (SceneViewModel)Session.GetAssetById(newParentReference.Id);
                    if (newParent != null)
                        // Note: this should only have effect when replacing/fixing up a reference.
                        newParent.Children.Add(this);
                    else
                        Parent = null;
                }
            }
            finally
            {
                updatingParent = false;
            }
        }

        private void ParentPropertyChanged()
        {
            if (updatingParent || UndoRedoService.UndoRedoInProgress)
                return;

            try
            {
                updatingParent = true;
                var newValue = parent != null ? AttachedReferenceManager.CreateProxyObject<Scene>(parent.Id, parent.AssetItem.Location) : null;
                parentNode.Update(newValue);
            }
            finally
            {
                updatingParent = false;
            }
        }

        private void SessionStateChanged(object sender, SessionStateChangedEventArgs e)
        {
            UpdateCommands();
        }

        private void SetAsDefault()
        {
            if (!SetAsDefaultCommand.IsEnabled)
                return;

            // TODO: find a better (faster?) way to access the game settings view model
            var gameSettings = Session.CurrentProject?.AllAssets.OfType<GameSettingsViewModel>().FirstOrDefault();
            if (gameSettings == null)
                return;
            gameSettings.DefaultScene = this;
        }

        private void UpdateCommands()
        {
            // Cannot set scene as default if
            // 1. session does not have a current package (not executable game)
            // 2. scene is not reachable from the main package (not a dependency), i.e. not in Package.AllAssets
            SetAsDefaultCommand.IsEnabled = Session.CurrentProject?.AllAssets.OfType<SceneViewModel>().Contains(this) ?? false;
        }
    }
}
