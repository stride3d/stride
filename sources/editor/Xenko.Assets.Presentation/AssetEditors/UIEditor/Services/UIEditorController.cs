// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Quantum.Visitors;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Mathematics;
using Xenko.Core.Quantum;
using Xenko.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.PrefabEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.UIEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.UIEditor.ViewModels;
using Xenko.Assets.SpriteFont;
using Xenko.Assets.SpriteFont.Compiler;
using Xenko.Assets.UI;
using Xenko.Editor.EditorGame.Game;
using Xenko.Engine;
using Xenko.Graphics.Font;
using Xenko.Rendering;
using Xenko.Shaders.Compiler;
using Xenko.UI;
using Xenko.UI.Controls;
using Xenko.UI.Panels;

namespace Xenko.Assets.Presentation.AssetEditors.UIEditor.Services
{
    /// <summary>
    /// Game controller for the UI editor.
    /// </summary>
    /// <seealso cref="UIEditorBaseViewModel"/>
    public abstract class UIEditorController : AssetCompositeHierarchyEditorController<PrefabEditorGame, UIElementDesign, UIElement, UIHierarchyItemViewModel>
    {
        internal const float DesignDensity = 100.0f;

        internal const string AdornerEntityName = "AdornerEntity";
        internal const string AdornerRootElementName = "AdornerRoot";
        internal const string DesignAreaEntityName = "DesignAreaEntity";
        internal const string DesignAreaRootElementName = "DesignAreaRoot";
        internal const string UIEntityName = "UIEntity";

        private readonly Lazy<Graphics.SpriteFont> defaultFontLazy;
        private Vector3 resolution;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIEditorController"/> class.
        /// </summary>
        /// <param name="asset">The asset associated with this instance.</param>
        /// <param name="editor">The editor associated with this instance.</param>
        protected UIEditorController([NotNull] AssetViewModel asset, [NotNull] UIEditorBaseViewModel editor)
            : base(asset, editor, CreateEditorGame)
        {
            // move this in a service shared by all editor controllers
            defaultFontLazy = new Lazy<Graphics.SpriteFont>(() =>
            {
                var fontItem = SignedDistanceFieldSpriteFontFactory.Create();
                fontItem.FontType.Size = 16.0f;
                return SignedDistanceFieldFontCompiler.Compile(Game.Services.GetService<IFontFactory>(), fontItem);
            });

            var resolutionNode = Editor.NodeContainer.GetNode(((UIAssetBase)Asset.Asset).Design)[nameof(UIAssetBase.UIDesign.Resolution)];
            resolutionNode.ValueChanged += ResolutionChanged;
            resolution = (Vector3)resolutionNode.Retrieve();
        }

        private async void ResolutionChanged(object sender, MemberNodeChangeEventArgs e)
        {
            resolution = Vector3.Max((Vector3)e.NewValue, Vector3.One);
            await InvokeAsync(() =>
            {
                var size = resolution / DesignDensity;
                var uiComponent = GetEntityByName(DesignAreaEntityName)?.Get<UIComponent>();
                if (uiComponent != null)
                {
                    uiComponent.Resolution = resolution;
                    uiComponent.Size = size;
                }
                uiComponent = GetEntityByName(UIEntityName)?.Get<UIComponent>();
                if (uiComponent != null)
                {
                    uiComponent.Resolution = resolution;
                    uiComponent.Size = size;
                }
                uiComponent = GetEntityByName(AdornerEntityName)?.Get<UIComponent>();
                if (uiComponent != null)
                {
                    uiComponent.Resolution = resolution*2;
                    uiComponent.Size = size*2;
                }
                AdornerService.Refresh().Forget();
            });
        }

        internal UIEditorGameAdornerService AdornerService { get; private set; }

        internal Graphics.SpriteFont DefaultFont => defaultFontLazy.Value;

        internal new UIEditorBaseViewModel Editor  => (UIEditorBaseViewModel)base.Editor;

        private Dictionary<Guid, UIElement> RootElements { get; } = new Dictionary<Guid, UIElement>();

        private UIAssetBase.UIDesign uiDesign;

        /// <inheritdoc/>
        public override async Task<bool> CreateScene()
        {
            ICollection<UIElement> rootElements;
            if (!ConstructRootElements(out rootElements, out uiDesign))
                return false;

            rootElements.ForEach(r => RootElements[r.Id] = r);

            var resolution = uiDesign.Resolution;
            var size = resolution / DesignDensity;
            var rootEntity = new Entity(UIEntityName)
            {
                new UIComponent
                {
                    Page = new UIPage { RootElement = rootElements.FirstOrDefault() },
                    IsBillboard = false,
                    IsFullScreen = false,
                    Resolution = resolution,
                    ResolutionStretch = ResolutionStretch.FixedWidthFixedHeight,
                    Size = size,
                }
            };
            var designArea = new Entity(DesignAreaEntityName)
            {
                new UIComponent
                {
                    Page = new UIPage
                    {
                        RootElement = new Border
                        {
                            Name = DesignAreaRootElementName,
                            BackgroundColor = Color.WhiteSmoke * 0.5f, //FIXME: add an editor setting
                            BorderColor = Color.WhiteSmoke, // FIXME: add an editor setting
                            BorderThickness = Thickness.UniformCuboid(2.0f), // FIXME: add an editor setting
                        }
                    },
                    IsBillboard = false,
                    IsFullScreen = false,
                    Resolution = resolution,
                    ResolutionStretch = ResolutionStretch.FixedWidthFixedHeight,
                    Size = size,
                }
            };
            designArea.Transform.Position.Z = -10;
            var uiAdorners = new Entity(AdornerEntityName)
            {
                new UIComponent
                {
                    Page = new UIPage
                    {
                        RootElement = new Canvas
                        {
                            CanBeHitByUser = true,
                            Name = AdornerRootElementName,
                            Visibility = Visibility.Visible,
                        }
                    },
                    IsBillboard = false,
                    IsFullScreen = false,
                    Resolution = resolution*2,
                    ResolutionStretch = ResolutionStretch.FixedWidthFixedHeight,
                    Size = size*2,
                }
            };
            uiAdorners.Transform.Position.Z = 10;

            var entities = new[] { designArea, rootEntity, uiAdorners };
            await InvokeAsync(() =>
            {
                Game.InitializeContentScene();
                Game.LoadEntities(entities);
            });
            RecoveryService.IsActive = true;
            return true;
        }

        protected virtual bool ConstructRootElements(out ICollection<UIElement> rootElements, out UIAssetBase.UIDesign editorSettings)
        {
            var uiAsset = (UIAssetBase)AssetCloner.Clone(Asset.Asset, AssetClonerFlags.ReferenceAsNull);
            editorSettings = uiAsset.Design;
            var elements = new List<UIElement>();
            foreach (var part in uiAsset.Hierarchy.RootParts)
            {
                UIElementDesign elementDesign;
                if (uiAsset.Hierarchy.Parts.TryGetValue(part.Id, out elementDesign))
                {
                    elements.Add(elementDesign.UIElement);
                }
            }

            rootElements = elements;
            return true;
        }

        /// <inheritdoc />
        public override Task AddPart([NotNull] UIHierarchyItemViewModel parent, UIElement assetSidePart)
        {
            EnsureAssetAccess();

            var gameSidePart = ClonePartForGameSide(parent.Asset.Asset, assetSidePart);
            return InvokeAsync(() =>
            {
                Logger.Debug($"Adding element {assetSidePart.Id} to game-side scene");

                var parentId = (parent as UIElementViewModel)?.Id;
                if (parentId == null)
                {
                    RootElements[assetSidePart.Id] = gameSidePart;
                }
                else
                {
                    var parentElement = (UIElement)FindPart(parentId.Value);
                    if (parentElement == null)
                        throw new InvalidOperationException($"The given {nameof(parentId)} does not correspond to any existing part.");

                    var panel = parentElement as Panel;
                    var contentControl = parentElement as ContentControl;
                    if (panel != null)
                    {
                        GameSideNodeContainer.GetNode(panel.Children).Add(gameSidePart);
                    }
                    else if (contentControl != null)
                    {
                        if (contentControl.Content != null)
                        {
                            throw new InvalidOperationException($"The control corresponding to the given {nameof(parentId)} is a ContentControl that already has a Content.");
                        }
                        GameSideNodeContainer.GetNode(contentControl)[nameof(contentControl.Content)].Update(gameSidePart);
                    }
                }
            });
        }

        /// <inheritdoc />
        public override Task RemovePart([NotNull] UIHierarchyItemViewModel parent, UIElement assetSidePart)
        {
            EnsureAssetAccess();

            return InvokeAsync(() =>
            {
                Logger.Debug($"Removing element {assetSidePart.Id} from game-side scene");
                var partId = new AbsoluteId(AssetId.Empty, assetSidePart.Id);
                var part = (UIElement)FindPart(partId);
                if (part == null)
                    throw new InvalidOperationException($"The given {nameof(assetSidePart.Id)} does not correspond to any existing part.");

                var parentId = (parent as UIElementViewModel)?.Id;
                if (parentId == null)
                {
                    RootElements.Remove(assetSidePart.Id);
                }
                else
                {
                    var parentElement = (UIElement)FindPart(parentId.Value);
                    if (parentElement == null)
                        throw new InvalidOperationException($"The given {nameof(parentId)} does not correspond to any existing part.");

                    var panel = parentElement as Panel;
                    var contentControl = parentElement as ContentControl;
                    if (panel != null)
                    {
                        var i = panel.Children.IndexOf(part);
                        GameSideNodeContainer.GetNode(panel.Children).Remove(part, new NodeIndex(i));
                    }
                    else if (contentControl != null)
                    {
                        if (contentControl.Content != null)
                            throw new InvalidOperationException($"The control corresponding to the given {nameof(parentId)} is a ContentControl that already has a Content.");
                        GameSideNodeContainer.GetNode(contentControl)[nameof(contentControl.Content)].Update(null);
                    }
                }
            });
        }

        /// <summary>
        /// Finds the game-side <see cref="UIElement"/> corresponding to the given <paramref name="elementId"/>.
        /// </summary>
        /// <remarks>
        /// This method will search through all roots. If the root is known , consider calling <see cref="FindElement(System.Guid,System.Guid)"/> instead.
        /// </remarks>
        /// <param name="elementId">The identifier of the game-side element to find.</param>
        /// <returns>A game-side element corresponding to the given id if found, <c>null</c> otherwise.</returns>
        protected override object FindPart(AbsoluteId elementId)
        {
            return RootElements.Keys.Select(rootId => FindElement(rootId, elementId.ObjectId)).FirstOrDefault(x => x != null);
        }

        /// <summary>
        /// Finds the game-side <see cref="UIElement"/> corresponding to the given <paramref name="elementId"/>.
        /// </summary>
        /// <param name="rootId">The identifier of the root where the element should be found.</param>
        /// <param name="elementId">The identifier of the game-side element to find.</param>
        /// <returns>A game-side element corresponding to the given id if found, <c>null</c> otherwise.</returns>
        [CanBeNull]
        public UIElement FindElement(Guid rootId, Guid elementId)
        {
            UIElement rootElement;
            return RootElements.TryGetValue(rootId, out rootElement) ? FindElementDeep(rootElement, elementId) : null;
        }

        public void HideUI()
        {
            EnsureGameAccess();

            var uiComponent = GetUIComponent();
            if (uiComponent != null)
            {
                uiComponent.Enabled = false;
                uiComponent.Page.RootElement = null;
            }
        }

        public void RemoveRootElement(Guid rootId)
        {
            EnsureGameAccess();

            RootElements.Remove(rootId);
            var uiComponent = GetUIComponent();
            if (uiComponent?.Page.RootElement?.Id == rootId)
            {
                uiComponent.Enabled = false;
                uiComponent.Page.RootElement = null;
            }
        }

        public void ShowUI(Guid rootId)
        {
            EnsureGameAccess();

            var uiComponent = GetUIComponent();
            if (uiComponent != null)
            {
                uiComponent.Enabled = true;
                uiComponent.Page.RootElement = RootElements[rootId];
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(UIEditorController));

            var resolutionNode = Editor.NodeContainer.GetNode(((UIAssetBase)Asset.Asset).Design)[nameof(UIAssetBase.UIDesign.Resolution)];
            resolutionNode.ValueChanged -= ResolutionChanged;

            base.Destroy();
        }

        /// <inheritdoc/>
        protected override void InitializeServices(EditorGameServiceRegistry services)
        {
            base.InitializeServices(services);

            services.Add(new UIEditorGameCameraService(this));
            services.Add(AdornerService = new UIEditorGameAdornerService(this));
        }

        /// <inheritdoc/>
        protected override Dictionary<Guid, IIdentifiable> CollectIdentifiableObjects()
        {
            var allElements = RootElements.Values.BreadthFirst(x => x.VisualChildren);
            var definition = AssetQuantumRegistry.GetDefinition(Asset.Asset.GetType());
            var identifiableObjects = new Dictionary<Guid, IIdentifiable>();
            foreach (var entityNode in allElements.Select(x => GameSideNodeContainer.GetOrCreateNode(x)))
            {
                foreach (var identifiable in IdentifiableObjectCollector.Collect(definition, entityNode))
                {
                    identifiableObjects.Add(identifiable.Key, identifiable.Value);
                }
            }
            return identifiableObjects;
        }

        /// <summary>
        /// Gets the entity identified by its <paramref name="name"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [CanBeNull]
        internal Entity GetEntityByName(string name)
        {
            return Game.ContentScene?.Entities.FirstOrDefault(e => e.Name == name);
        }

        public Vector3 GetMousePositionInUI()
        {
            var scenePosition = GetMousePositionInScene(false);

            var uiEntity = GetEntityByName(AdornerEntityName);
            var uiComponent = uiEntity.Get<UIComponent>();
            var worldMatrix = Matrix.Scaling(uiComponent.Size / uiComponent.Resolution) * uiEntity.Transform.WorldMatrix;
            // Rotation of Pi along 0x to go from UI space to world space
            worldMatrix.Row2 = -worldMatrix.Row2;
            worldMatrix.Row3 = -worldMatrix.Row3;

            Matrix matrixInv;
            Matrix.Invert(ref worldMatrix, out matrixInv);

            Vector3 result;
            Vector3.TransformCoordinate(ref scenePosition, ref matrixInv, out result);
            return result;
        }

        [NotNull]
        private static PrefabEditorGame CreateEditorGame(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string effectLogPath)
        {
            return new PrefabEditorGame(gameContentLoadedTaskSource, effectCompiler, effectLogPath);
        }

        [CanBeNull]
        private UIComponent GetUIComponent()
        {
            return GetEntityByName(UIEntityName)?.Get<UIComponent>();
        }

        /// <summary>
        /// Iterates through the visual tree of <paramref name="element"/> and returns the first element matching the <paramref name="elementId"/>.
        /// </summary>
        /// <param name="element">The root of the sub-tree to search through.</param>
        /// <param name="elementId">The id of the element to look for.</param>
        /// <returns>The first element matching the id in the sub-tree or <c>null</c> if not found.</returns>
        [CanBeNull]
        private static UIElement FindElementDeep([NotNull] UIElement element, Guid elementId)
        {
            if (element.Id == elementId)
                return element;

            return element.VisualChildren.BreadthFirst(e => e.VisualChildren).FirstOrDefault(e => e.Id == elementId);
        }
    }
}
