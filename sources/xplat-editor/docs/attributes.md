# Attributes and types registration

There are a few changes regarding how we declare which view model types represent which asset type, and the same for editors and previews.

## Attributes

### Current editor

Previously, we had `AssetViewModelAttribute`, `AssetEditorViewModelAttribute`, `AssetPreviewAttribute` and , `AssetPreviewViewModelAttribute`.

`AssetViewModelAttribute` was attached to view models and indicated the type of asset associated with it:
```csharp
[AssetViewModel(typeof(PrefabAsset))]
public class PrefabViewModel : EntityHierarchyViewModel {}
```

`AssetEditorViewModelAttribute` was attached to editor view models and indicated both the type of asset and the type for the editor view:
```csharp
[AssetEditorViewModel(typeof(PrefabAsset), typeof(PrefabEditorView))]
public sealed class PrefabEditorViewModel : EntityHierarchyEditorViewModel {}
```
This is problematic because it creates a tight coupling with the editor view (which is UI-dependent and here is implemented using WPF) with the (editor) view model, while both are supposed to be separated (that's the whole point of the MVVM pattern).

The same issue existed with `AssetPreviewAttribute` which associates the type of asset with the view type for a preview of that asset:
```csharp
[AssetPreview(typeof(PrefabAsset), typeof(ModelPreviewView))]
public class PrefabPreview : PreviewFromEntity<PrefabAsset> {}
```
In addition with the coupling issue, that last attribute was defined in the `Stride.Editor` assembly, and not in `Stride.Core.Assets.Editor` like the two others. Thus, on top of that it was made dependent on the Stride runtime. Ideally it should only depends on the Core libraries.

`AssetPreviewViewModelAttribute` was attached to the view model of the preview and indicated the type of the preview. That association was not problematic.
```csharp
[AssetPreviewViewModel(typeof(EntityPreview))]
public class EntityPreviewViewModel : AssetPreviewViewModel {}
```

### What changed?

There are now, not 4, but 6 attributes: `AssetViewModelAttribute`, `AssetEditorViewModelAttribute`, `AssetEditorViewAttribute`, `AssetPreviewAttribute`, `AssetPreviewViewAttribute` and `AssetPreviewViewModelAttribute`.

`AssetViewModelAttribute` and `AssetPreviewViewModelAttribute` are mostly unchanged as they didn't create any dependency issue. They got a minor upgrade though, as current version of the C# language now supports closed generics:
```csharp
public sealed class AssetViewModelAttribute<T>
    where T : Asset {}

[AssetViewModel<PrefabAsset>]
public sealed class PrefabViewModel : EntityHierarchyViewModel {}
```

All other 4 attributes have different definitions in order to break their incorrect dependencies:
```csharp
[AssetEditorViewModel<PrefabViewModel>]
public sealed class PrefabEditorViewModel : EntityHierarchyEditorViewModel {}

[AssetEditorView<EntityHierarchyEditorViewModel>]
public partial class EntityHierarchyEditorView : UserControl {}

[AssetPreview<PrefabAsset>]
public class PrefabPreview : PreviewFromEntity<PrefabAsset> {}

[AssetPreviewViewAttribute<ModelPreview>]
[AssetPreviewViewAttribute<PrefabPreview>]
[AssetPreviewViewAttribute<PrefabModelPreview>]
[AssetPreviewViewAttribute<ProceduralModelPreview>]
[AssetPreviewViewAttribute<SpriteStudioSheetPreview>]
public class ModelPreviewView : StridePreviewView {}

[AssetPreviewViewModel<EntityPreview>]
public sealed class EntityPreviewViewModel : AssetPreviewViewModel<EntityPreview> {}
```
Now the editor view model only indicates the view model it is editing, independently on what the actual UI would be. That editor is defined on the UI-implementing class and connects it to the editor view model. And for the preview, similar links exists. Note that a concrete view can be reused for multiple preview types.

In summary, we now have the proper dependency links:
```
asset type <- asset view model type <- asset editor view model type <- asset editor type
asset type <- asset preview type <- asset preview view type
                                 |- asset preview view model type
```

## Types registration

The types annotated and defined by these attributes get registered through the plugin system.

The idea is that different assemblies can define view models, editors, previews, etc. for assets, without being directly referenced by the Game Studio. They act as external plugins, the same way 3rd-party could later add their own assets and editor support.

*Note: currently the plugin architecture is not completed, so we do have a direct dependency to those assemblies. However, it is very loose (purposely) ; and once completed, the same mechanisms (or very similar ones) will be used.*

### Asset view models

An asset view model type is associated to an asset type.

Asset view model type registration:
```csharp
// in PluginService.cs
var registeredAssetViewModelsTypes = new Dictionary<Type, Type>();
plugin.RegisterAssetViewModelTypes(registeredAssetViewModelsTypes);
AssertType(typeof(Asset), registeredAssetViewModelsTypes.Select(x => x.Key));
AssertType(typeof(AssetViewModel), registeredAssetViewModelsTypes.Select(x => x.Value));
assetViewModelTypes.AddRange(registeredAssetViewModelsTypes);

// in AssetsPlugin.cs
void RegisterAssetViewModelTypes(IDictionary<Type, Type> assetViewModelTypes)
{
    var pluginAssembly = GetType().Assembly;
    foreach (var type in pluginAssembly.GetTypes())
    {
        if (typeof(AssetViewModel).IsAssignableFrom(type) &&
            type.GetCustomAttribute<AssetViewModelAttribute>() is { } attribute)
        {
            assetViewModelTypes.Add(attribute.AssetType, type);
        }
    }
}
```

Asset view model creation:
```csharp
// in PackageViewModel.cs
var assetViewModelType = Session.GetAssetViewModelType(assetItem); // calls PluginService.GetAssetViewModelType()
if (assetViewModelType.IsGenericType)
{
    assetViewModelType = assetViewModelType.MakeGenericType(assetItem.Asset.GetType());
}
return (AssetViewModel)Activator.CreateInstance(assetViewModelType, new ConstructorParameters(assetItem, directory, false))!;
```

### Asset editor view models

An asset editor view model type is associated to an asset view model type.

Asset editor view model type registration:
```csharp
// in PluginService.cs
var registeredAssetEditorViewModelsTypes = new Dictionary<Type, Type>();
editorPlugin.RegisterAssetEditorViewModelTypes(registeredAssetEditorViewModelsTypes);
AssertType(typeof(AssetViewModel), registeredAssetEditorViewModelsTypes.Select(x => x.Key));
AssertType(typeof(AssetEditorViewModel), registeredAssetEditorViewModelsTypes.Select(x => x.Value));
editorViewModelTypes.AddRange(registeredAssetEditorViewModelsTypes);

// in AssetsEditorPlugin.cs
void RegisterAssetEditorViewModelTypes(IDictionary<Type, Type> assetEditorViewTypes)
{
    var pluginAssembly = GetType().Assembly;
    foreach (var type in pluginAssembly.GetTypes())
    {
        if (typeof(AssetEditorViewModel).IsAssignableFrom(type) &&
            type.GetCustomAttribute<AssetEditorViewModelAttribute>() is { } attribute)
        {
            assetEditorViewTypes.Add(attribute.EditorViewModelType, type);
        }
    }
}
```

Asset editor view model creation:
```csharp
// in EditorCollectionViewModel.cs
var editorType = ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewModelType(asset.GetType());
if (editorType is not null)
{
    editor = (AssetEditorViewModel)Activator.CreateInstance(editorType, asset)!;
    openedAssets.Add(asset, editor);
    editors.Add(editor);
    ActiveEditor = editor;
}
```

### Asset editor views

An asset editor view type is associated to an asset editor view model type.

Asset editor view type registration:
```csharp
// in PluginService.cs
var registeredAssetEditorViewTypes = new Dictionary<Type, Type>();
editorPlugin.RegisterAssetEditorViewTypes(registeredAssetEditorViewTypes);
AssertType(typeof(AssetEditorViewModel), registeredAssetEditorViewTypes.Select(x => x.Key));
AssertType(typeof(IAssetEditorView), registeredAssetEditorViewTypes.Select(x => x.Value));
editorViewTypes.AddRange(registeredAssetEditorViewTypes);

// In AssetsEditorPlugin.cs
void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
{
    var pluginAssembly = GetType().Assembly;
    foreach (var type in pluginAssembly.GetTypes())
    {
        if (typeof(IAssetEditorView).IsAssignableFrom(type) &&
            type.GetCustomAttribute<AssetEditorViewAttribute>() is { } attribute)
        {
            assetEditorViewTypes.Add(attribute.EditorViewModelType, type);
        }
    }
}
```

Asset editor view creation:
```csharp
// EditorViewSelector.cs
public Control? Build(object? param)
{
    if (param == null) return null;

    var viewType = Session?.ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewType(param.GetType());
    return viewType != null ? Activator.CreateInstance(viewType) as Control : null;
}
```

As you can see, this part is really dependent on the UI framework (in this case Avalonia). That's why the registration is done with an interface type (`IAssetEditorView`) and not a common abstract class. We want to keep the plugin system UI-agnostic, so that such registrations are possible. On the other hand of that system, when it is time to instantiate the final type, it happens in a context where the UI framework is known.

### Asset previews

An asset preview type is associated to an asset type.

Asset preview type registration:
```csharp
// In StrideEditorViewPlugin.cs
void InitializeSession(ISessionViewModel session)
{
    var pluginService = session.ServiceProvider.Get<IAssetsPluginService>();
    var previewFactories = new Dictionary<Type, AssetPreviewFactory>();
    foreach (var stridePlugin in pluginService.Plugins.OfType<AssetsEditorPlugin>())
    {
        var pluginAssembly = stridePlugin.GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(IAssetPreview).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetPreviewAttribute>() is { } attribute)
            {
                previewFactories.Add(attribute.AssetType, (builder, game, asset) => (IAssetPreview)Activator.CreateInstance(type)!);
            }
        }
    }

    var previewService = new GameStudioPreviewService(session);
    previewService.RegisterAssetPreviewFactories(previewFactories);
    session.ServiceProvider.RegisterService(previewService);
}
```

In this particular case, we are not registering through PluginService, but directly in the plugin class where the related service is defined. This was done for convenience, but could change in the future to be more consistent with the other cases.

Asset preview creation:
```csharp
IAssetPreview GetPreviewForAsset(AssetViewModel asset)
{
    var assetType = asset.Asset.GetType();
    while (assetType is not null)
    {
        AssetPreviewFactory factory;
        if (assetPreviewFactories.TryGetValue(assetType, out factory))
        {
            var assetPreview = factory(this, PreviewGame, asset.AssetItem);
            return assetPreview;
        }
        assetType = assetType.BaseType;
    }
    return null;
}
```

### Asset preview views

An asset preview view type is associated to an asset preview type.

Asset preview view type registration:
```csharp
// in PluginService.cs
var registeredAssetPreviewViewTypes = new Dictionary<Type, Type>();
editorPlugin.RegisterAssetPreviewViewTypes(registeredAssetPreviewViewTypes);
AssertType(typeof(IAssetPreview), registeredAssetPreviewViewTypes.Select(x => x.Key));
AssertType(typeof(IPreviewView), registeredAssetPreviewViewTypes.Select(x => x.Value));
previewViewViewTypes.AddRange(registeredAssetPreviewViewTypes);

// In StrideEditorViewPlugin.cs
void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
{
    var pluginAssembly = GetType().Assembly;
    foreach (var type in pluginAssembly.GetTypes())
    {
        if (typeof(IPreviewView).IsAssignableFrom(type))
        {
            foreach (var attribute in type.GetCustomAttributes<AssetPreviewViewAttribute>())
            {
                assetPreviewViewTypes.Add(attribute.AssetPreviewType, type);
            }
        }
    }
}
```

Similarly to asset editor view types, we use interfaces to break the dependency to a specific UI framework.
However, contrary to other attributes we do allow multiple ones for the same preview.

Asset preview view creation:
```csharp
// In AssetPreview.cs
async Task<IPreviewView?> ProvideView(IViewModelServiceProvider serviceProvider)
{
    var pluginService = serviceProvider.Get<IAssetsPluginService>();
    var viewType = pluginService.GetPreviewViewType(GetType()) ?? DefaultViewType;

    return viewType is not null
        ? await Builder.Dispatcher.InvokeAsync(() =>
        {
            var view = (IPreviewView?)Activator.CreateInstance(viewType);
            view?.InitializeView(Builder, this);
            return view;
        })
        : null;
}
```

### Asset preview view models

An asset preview view model type is associated to an asset preview type.

Asset preview view model type registration:
```csharp
// in PluginService.cs
var registeredAssetPreviewViewModelTypes = new Dictionary<Type, Type>();
editorPlugin.RegisterAssetPreviewViewModelTypes(registeredAssetPreviewViewModelTypes);
AssertType(typeof(IAssetPreview), registeredAssetPreviewViewModelTypes.Select(x => x.Key));
AssertType(typeof(IAssetPreviewViewModel), registeredAssetPreviewViewModelTypes.Select(x => x.Value));
previewViewModelTypes.AddRange(registeredAssetPreviewViewModelTypes);

// In StrideEditorPlugin.cs
void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
{
    var pluginAssembly = GetType().Assembly;
    foreach (var type in pluginAssembly.GetTypes())
    {
        if (typeof(IAssetPreviewViewModel).IsAssignableFrom(type) &&
            type.GetCustomAttribute<AssetPreviewViewModelAttribute>() is { } attribute)
        {
            assetPreviewViewModelTypes.Add(attribute.AssetPreviewType, type);
        }
    }
}
```

Asset preview view model creation:
```csharp
// In AssetPreview.cs
async Task<IAssetPreviewViewModel?> ProvideViewModel(IViewModelServiceProvider serviceProvider)
{
    var pluginService = serviceProvider.Get<IAssetsPluginService>();
    var previewViewModelType = pluginService.GetPreviewViewModelType(GetType());

    return previewViewModelType is not null
        ? await AssetViewModel.Dispatcher.InvokeAsync(() =>
        {
            return (IAssetPreviewViewModel?)Activator.CreateInstance(previewViewModelType, AssetViewModel.Session);
        })
        : null;
}
```
