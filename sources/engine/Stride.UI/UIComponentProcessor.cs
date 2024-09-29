// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Games;

namespace Stride.UI;

/// <summary>
/// The processor in charge of updating entities that have <see cref="UIComponent"/>s.
/// </summary>
public class UIComponentProcessor : EntityProcessor<UIComponent, UIDocument>
{
    private UISystem uiSystem;
    
    public UIComponentProcessor() : base(typeof(TransformComponent))
    {
        
    }

    protected override void OnSystemAdd()
    {
        uiSystem = Services.GetService<UISystem>();
        if (uiSystem == null)
        {
            uiSystem = new UISystem(Services);
            Services.AddService(uiSystem);
            var gameSystems = Services.GetService<IGameSystemCollection>();
            gameSystems?.Add(uiSystem);
        }
    }

    public override void Update(GameTime time)
    {
        // Update all the UI documents that are created from UIComponents to match their values.
        foreach (var componentDocumentKeyPair in ComponentDatas)
        {
            var uiComponent = componentDocumentKeyPair.Key;
            var uiDocument = componentDocumentKeyPair.Value;

            uiDocument.Enabled = uiComponent.Enabled;
            
            uiDocument.WorldMatrix = uiComponent.Entity.Transform.WorldMatrix;
            uiDocument.RenderGroup = uiComponent.RenderGroup;
            uiDocument.Page = uiComponent.Page;
            uiDocument.Sampler = uiComponent.Sampler;
            uiDocument.IsFullScreen = uiComponent.IsFullScreen;
            uiDocument.Resolution = uiComponent.Resolution;
            uiDocument.Size = uiComponent.Size;
            uiDocument.ResolutionStretch = uiComponent.ResolutionStretch;
            uiDocument.IsBillboard = uiComponent.IsBillboard;
            uiDocument.SnapText = uiComponent.SnapText;
            uiDocument.IsFixedSize = uiComponent.IsFixedSize;
        }
    }

    protected override void OnEntityComponentAdding(Entity entity, UIComponent uiComponent, UIDocument uiDocument)
    {
        uiSystem.AddDocument(uiDocument);
    }

    protected override void OnEntityComponentRemoved(Entity entity, UIComponent uiComponent, UIDocument uiDocument)
    {
        uiSystem.RemoveDocument(uiDocument);
    }

    protected override UIDocument GenerateComponentData(Entity entity, UIComponent component)
    {
        return new UIDocument();
    }
}
