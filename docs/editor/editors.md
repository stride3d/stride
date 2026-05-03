# Existing Asset Editors

## Overview

All concrete asset editors live in `Stride.Assets.Presentation` under `sources/editor/Stride.Assets.Presentation/AssetEditors/`. Most editor folders contain a `ViewModels/` subdirectory and a `Views/` subdirectory; ScriptEditor and VisualScriptEditor are exceptions where files sit flat at the folder root. The table below is the entry point for locating any existing editor.

## Editors

| Editor | Asset type | Base class | Game viewport | Folder |
|---|---|---|---|---|
| `SpriteSheetEditorViewModel` | `SpriteSheetAsset` | `AssetEditorViewModel` | No | `SpriteEditor/` |
| `SceneEditorViewModel` | `SceneAsset` | `EntityHierarchyEditorViewModel` | Yes | `EntityHierarchyEditor/` |
| `PrefabEditorViewModel` | `PrefabAsset` | `EntityHierarchyEditorViewModel` | Yes | `EntityHierarchyEditor/` |
| `UIPageEditorViewModel` | `UIPageAsset` | `AssetCompositeHierarchyEditorViewModel` | Yes | `UIPageEditor/` |
| `UILibraryEditorViewModel` | `UILibraryAsset` | `AssetCompositeHierarchyEditorViewModel` | Yes | `UILibraryEditor/` |
| `GraphicsCompositorEditorViewModel` | `GraphicsCompositorAsset` | `AssetEditorViewModel` | No | `GraphicsCompositorEditor/` |
| `ScriptEditorViewModel` | Script assets | `AssetEditorViewModel` | No | `ScriptEditor/` |
| `VisualScriptEditorViewModel` | `VisualScriptAsset` | `AssetEditorViewModel` | No | `VisualScriptEditor/` |

### SpriteSheetEditorViewModel

**What it does:** Lets the user define sprites within a texture — regions, pivot points, borders, and animation frames. Renders a preview using its own lightweight `ViewportViewModel` rather than a full game instance.

**Key types:**

| Class | Role |
|---|---|
| `SpriteSheetEditorViewModel` | Editor ViewModel |
| `SpriteEditorView` | XAML view |
| `ViewportViewModel` | Lightweight render preview (no full game loop) |

**Notable:**
- Uses `ViewportViewModel` for rendering instead of `IEditorGameController` — lighter than a full game editor.
- Implements `IAddChildViewModel` to support dragging textures onto the editor to add new sprites.

### SceneEditorViewModel / PrefabEditorViewModel

**What it does:** Full 3D scene and prefab editing with an entity hierarchy tree, transform gizmos, camera controls, and a live game viewport. Scene and prefab share the same base infrastructure with thin concrete subclasses.

**Key types:**

| Class | Role |
|---|---|
| `EntityHierarchyEditorViewModel` | Shared base ViewModel (`EntityHierarchyEditor/ViewModels/`) |
| `SceneEditorViewModel` | Thin subclass for scenes |
| `PrefabEditorViewModel` | Thin subclass for prefabs |
| `EntityViewModel` | Part ViewModel for each entity in the hierarchy |
| `EditorCameraViewModel` | Camera movement and controls (lives in `GameEditor/ViewModels/`, shared infrastructure) |
| `EntityGizmosViewModel` | Gizmo overlay (translate/rotate/scale handles) |
| `EntityHierarchyEditorView` | Abstract base view |
| `SceneEditorView` / `PrefabEditorView` | Concrete views |

**Notable:**
- Most logic is in `EntityHierarchyEditorViewModel`; the concrete subclasses are thin.
- Scene and prefab differ primarily in which hierarchy root they load and whether archetype (prefab base) linking is active.
- The view code-behind injects the game host into the XAML panel: `SceneView.Content = hierarchyEditor.Controller.EditorHost`.

### UIPageEditorViewModel / UILibraryEditorViewModel

**What it does:** WYSIWYG editing of UI hierarchies — pages (full-screen layouts) and libraries (reusable component collections). Renders elements in a live game viewport with selection adorners, resize handles, and snap guidelines.

**Key types:**

| Class | Role |
|---|---|
| `UIEditorBaseViewModel` | Shared base ViewModel (`UIEditor/ViewModels/`) |
| `UIPageEditorViewModel` | Subclass for UI pages |
| `UILibraryEditorViewModel` | Subclass for UI libraries |
| `UIElementViewModel` | Part ViewModel for each UI element |
| `UIEditorView` | Abstract base view (`UIEditor/Views/`) |
| `UIPageEditorView` / `UILibraryEditorView` | Concrete views |

**Notable:**
- The adorner overlay (guidelines, resize handles) is rendered as a WPF layer on top of the game viewport — one of the few places where WPF and game rendering are explicitly composited.
- `UIEditorBaseViewModel` handles element factories, zoom/pan state, and selection; subclasses add only asset-type-specific root handling.

### GraphicsCompositorEditorViewModel

**What it does:** Visual node-graph editor for the render pipeline. Nodes represent render features and render stages; edges represent data flow between them.

**Key types:**

| Class | Role |
|---|---|
| `GraphicsCompositorEditorViewModel` | Editor ViewModel (`GraphicsCompositorEditor/ViewModels/`) |
| `GraphicsCompositorEditorView` | XAML view |
| `SharedRendererFactoryViewModel` | Factory/list for shared renderer blocks |
| `RenderStageViewModel` | Node ViewModel for render stages |

**Notable:**
- Uses `Stride.Core.Presentation.Graph` for the WPF node-graph canvas.
- Does **not** use `IEditorGameController` — no live game instance; the compositor is a pure data-structure editor.

### ScriptEditorViewModel

**What it does:** Opens a script asset in an embedded code editor. Provides compilation feedback and basic IDE integration within GameStudio.

**Key types:**

| Class | Role |
|---|---|
| `ScriptEditorViewModel` | Editor ViewModel (`ScriptEditor/`) |
| `ScriptEditorView` | XAML view |

**Notable:**
- The lightest editor — mostly a shell around the embedded code editor control.
- Undo/redo is delegated to the code editor itself rather than `IUndoRedoService`; the standard transaction infrastructure does not apply here.

### VisualScriptEditorViewModel

**What it does:** Node-graph editor for visual scripting. Blocks represent operations; edges represent data and control flow between them.

**Key types:**

| Class | Role |
|---|---|
| `VisualScriptEditorViewModel` | Editor ViewModel (`VisualScriptEditor/`) |
| `VisualScriptMethodEditorViewModel` | Per-method graph editing |
| `VisualScriptBlockViewModel` | Node ViewModel for each block |
| `VisualScriptLinkViewModel` | Edge ViewModel for each connection |

**Notable:**
- Similar to `GraphicsCompositorEditorViewModel` in structure: a pure data-structure editor with no game instance.
- ViewModel files sit at the folder root (no `ViewModels/` subdir); views are in `Views/` as usual.

## Shared Game Editor Infrastructure

All game-viewport editors (Scene, Prefab, UIPage, UILibrary) inherit from `GameEditorViewModel` and run a game instance via `IEditorGameController`. The controller manages the game loop on a background thread; results are marshalled back to the ViewModel via `IDispatcherService`.

**Key implication for contributors:** property changes in these editors can originate from either the UI thread (user interaction) or the game thread (simulation update). Code that modifies ViewModel state from the game thread must dispatch to the UI thread via `Asset.Dispatcher` (`AssetEditorViewModel.Asset` inherits `Dispatcher` from `DispatcherViewModel`):

```csharp
Asset.Dispatcher.InvokeAsync(() =>
{
    // safe to update ViewModel properties here
});
```
