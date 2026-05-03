# Stride GameStudio — Cross-platform Editor: Analysis & Roadmap

> **Branch:** `xplat-editor`
> **Baseline reference:** commit `9ea93a6f8390c84e3bdb755430676d8e808c9625` (last master merge)
> **Scope:** `sources/presentation` and `sources/editor` only (engine projects excluded)

---

## 1. Goal & Approach

The objective is to port the WPF-based GameStudio to [Avalonia](https://avaloniaui.net/) to make it a cross-platform desktop application (Windows, Linux, macOS).

The approach imposes a clean **three-tier project structure**:

| Tier | Naming convention | Rule |
|------|-------------------|------|
| **UI-agnostic** | `Stride.Foo.Bar` | Only ViewModels, commands, services interfaces. No dependency on WPF, Avalonia, or any other UI framework. |
| **WPF** | `Stride.Foo.Bar.Wpf` | WPF-specific views, controls, behaviors. Kept as-is from the master branch. |
| **Avalonia** | `Stride.Foo.Bar.Avalonia` | Avalonia-specific views, controls, behaviors. The target of the porting work. |

---

## 2. WPF Baseline

Before the porting effort, the master branch had **no meaningful separation** between UI-agnostic logic and WPF-specific code. Everything was bundled in three main projects:

| WPF project (master) | Files | Notes |
|---|---|---|
| `Stride.Core.Assets.Editor` | ~446 | WPF-coupled (`UseWPF=true`). Contained both ViewModels and WPF views/controls. Had AvalonDock dependency. |
| `Stride.Assets.Presentation` | ~936 | WPF-coupled. Contained all Stride-specific asset editors, gizmos, game editor controllers, templates, etc. |
| `Stride.GameStudio` | ~76 | WPF executable. Contained the main window, docking layout, crash report helpers, debug services, etc. |
| `Stride.Core.Presentation` | ~47 | Already mostly UI-agnostic (base ViewModels, commands, dirtiables). |
| `Stride.Core.Presentation.Wpf` | ~276 | WPF-specific controls, converters, behaviors, markup extensions. |

### Key WPF dependencies (not yet ported to Avalonia)
- **AvalonDock** — dockable panel system for flexible layout.
- **RoslynPad** — code editor with C# IntelliSense in the script editor.
- **AvalonEdit** — syntax-highlighted text editor (used inside RoslynPad).
- **SharpDX / ServiceWire** — game rendering and remote debugging channels.

---

## 3. Current State (xplat-editor branch)

### 3.1 Project inventory

#### Presentation layer (`sources/presentation`)

| Project | CS files | AXAML files | Tier | Status |
|---|---|---|---|---|
| `Stride.Core.Presentation` | 53 | 0 | Agnostic | ✅ Solid — ViewModels, commands, dirtiables, undo/redo, service interfaces |
| `Stride.Core.Presentation.Quantum` | 38 | 0 | Agnostic | ✅ Solid — NodeViewModel, presenter interfaces |
| `Stride.Core.Presentation.Avalonia` | 106 | 8 | Avalonia | 🟡 Substantial — good converter/control coverage, some gaps (see §3.2) |
| `Stride.Core.Presentation.Wpf` | 259 | 0 | WPF | ✅ Kept from master (reference) |
| `Stride.Core.Presentation.Wpf.Dialogs` | 6 | 0 | WPF | ✅ Kept from master |
| `Stride.Core.Presentation.Wpf.Graph` | 21 | 0 | WPF | ✅ Kept from master |
| `Stride.Core.Translation.Presentation.Wpf` | 6 | 0 | WPF | ✅ Kept from master |

#### Editor layer (`sources/editor`)

| Project | CS files | AXAML files | Tier | Status |
|---|---|---|---|---|
| `Stride.Core.Assets.Presentation` | 48 | 0 | Agnostic | ✅ Solid — `AssetViewModel`, session interface, asset node presenters |
| `Stride.Core.Assets.Editor` | 162 | 0 | Agnostic | ✅ Solid — session VM, asset collection VM, undo/redo VM, plugin/debug/dialog service interfaces |
| `Stride.Assets.Presentation` | 37 | 0 | Agnostic | ✅ Solid — Stride-specific asset ViewModels (entity hierarchy, component references) |
| `Stride.Assets.Editor` | 76 | 0 | Agnostic | ✅ Solid — all Stride asset editor ViewModels (scene, prefab, sprite sheet, UI page, visual script, etc.) and all preview ViewModels |
| `Stride.Editor` | 66 | 0 | Agnostic | ✅ Solid — game/thumbnail/preview support (build service, loader, preview game) |
| `Stride.Core.Assets.Editor.Avalonia` | 37 | 3 | Avalonia | 🟡 Property grid template providers; `PropertyViewTheme`, `ImageResources`, `DefaultPropertyTemplateProviders` |
| `Stride.Assets.Editor.Avalonia` | 27 | 8 | Avalonia | 🔴 Editor views are mostly stubs (see §3.3) |
| `Stride.Editor.Avalonia` | 3 | 0 | Avalonia | 🔴 Minimal — only `EmbeddedGameForm`, `GameStudioPreviewService`, `StridePreviewView` |
| `Stride.GameStudio.Avalonia` | 25 | 16 | Avalonia | 🟡 Working shell — see §3.4 |
| `Stride.GameStudio.Avalonia.Desktop` | 2 | 0 | Avalonia | ✅ Desktop entry point (Program.cs) |
| `Stride.Core.Assets.Editor.Wpf` | 322 | 0 | WPF | ✅ Kept from master |
| `Stride.Assets.Presentation.Wpf` | 573 | 0 | WPF | ✅ Kept from master |
| `Stride.Editor.Wpf` | 66 | 0 | WPF | ✅ Kept from master |
| `Stride.GameStudio` | 40 | 0 | WPF | ✅ Kept from master (WPF executable) |

### 3.2 `Stride.Core.Presentation.Avalonia` — what's done vs WPF

**Done (compared to `Stride.Core.Presentation.Wpf`):**
- Most value converters (~37 converters matching the WPF set)
- Most markup extensions (~18 extensions)
- Core editor controls: `PropertyView`, `NumericTextBox`, vector/matrix/quaternion editors, `DateTimeEditor`, `ColorEditor`, `SearchComboBox`, `ExpandableItemsControl`, `TextLogViewer`, `GameEngineHost`
- Behaviors: `BindCurrentToolTipStringBehavior`, `ButtonCloseWindowBehavior`, `CloseWindowBehavior`
- Themes: `EditorStyles`, `GeometryResources`, `HyperlinkStyle`, `SearchComboBoxStyle`, `TextLogViewerStyle`, `ToolBarStyle`
- Services: `DialogService`, `DispatcherService`
- Window helpers: `MessageBox`, `CheckedMessageBox`, `WindowHelper`

**Missing or not yet ported from WPF:**
- **Controls:** `FilteringComboBox` (with sort support), `ColorPicker`, `MarkdownTextBlock`, `ModalWindow`/`PopupModalWindow`, `TagControl`, `TreeView` (virtualized custom), `VirtualizingTilePanel`, `VirtualizingTreePanel`, `RotationEditor`, `ScaleBar`, `TrackerControl`, `CanvasView`
- **Behaviors:** `BindableSelectedItemsBehavior`, `ContainTextAdornerBehavior`, `DeferredBehaviorBase`, `TreeViewDragDropBehavior`, `PropertyViewDragDropBehavior`, and ~30 others across adorners, drag & drop, tree views
- **Services (agnostic placeholder):** File dialogs interface (`IFileOpenModalDialog`, `IFileSaveModalDialog`, `IFolderOpenModalDialog`) are still in the WPF-only `Stride.Core.Presentation.Wpf` `Services/` folder. The agnostic `Stride.Core.Presentation.Dialogs` project is empty.

### 3.3 `Stride.Assets.Editor.Avalonia` — asset editor views

All asset editor **ViewModels** already live in the agnostic `Stride.Assets.Editor` project. The Avalonia project needs to supply the **views** for them.

| Asset editor | Avalonia view status |
|---|---|
| Entity Hierarchy (Scene/Prefab) | 🔴 Stub — shows entity tree as text, no embedded game viewport, no gizmos |
| Graphics Compositor | 🔴 Stub — shows asset name only |
| Sprite Sheet | 🔴 Stub — shows asset name only |
| UI Page / UI Library | 🔴 Stub — shows asset name only |
| Visual Script | 🔴 Stub — shows asset name only |
| Script Source File | 🔴 Stub — shows asset name only; no code editor |
| Preview views (texture, model, material, entity, etc.) | 🔴 Stub — `StridePreviewView` control exists but no real rendering integration |

For comparison, the WPF `Stride.Assets.Presentation.Wpf` contains 279 files under `AssetEditors/` alone, and 33 files for previews.

**What's still in WPF only (and needs Avalonia equivalents):**
- `EditorGameController` — coordinates the embedded game instance inside an editor
- `EditorGameCameraService`, `EditorGameGridService`, `EditorGameDebugService` — game-side services
- Gizmos system (~43 files: camera, entity, transform, audio, background, etc.)
- Entity factories (12 factory types for adding entities/components)
- Curve editor (35 files for animation key-frame editing)
- Templates system (237 files in `Templates/` + `TemplateProviders/`)
- `AssemblyRecompiler` and `AssemblyReloading` — script hot-reload
- `CurveEditor`, `UIEditor` with adorners and live drag-resize

### 3.4 `Stride.GameStudio.Avalonia` — current shell

**What works:**
- Opens and closes a Stride project (session loading)
- Menu bar (File, Edit, Help) with functional commands
- Fixed grid layout: editor area, property grid, solution explorer, asset view, log/output tabs, asset references panel
- Undo/Redo, selection navigation (Previous/Next)
- Settings window (persisted via `IInternalSettings`)
- About window (backers, version info)
- Debug window with tabs (asset nodes, undo/redo, log)
- Asset explorer with basic filters and view options (recently ported)
- Markdown viewer (for license/changelogs)
- Progress window for long operations
- Theme support (light/dark via Avalonia Fluent theme)
- Clipboard service, Dialog service (Avalonia-based)
- `PluginService` that registers type mappings for ViewModels/Views

**What's missing vs WPF `Stride.GameStudio`:**
- **Docking layout** — WPF uses AvalonDock; the current Avalonia layout is a hard-coded `Grid` with `GridSplitter`s (explicitly deferred in the TODO)
- **Asset creation dialogs** — item template browser, new project wizard, package picker
- **Asset picker dialog** — modal window for browsing and selecting assets
- **Credentials dialog** — for package/service authentication
- **Build integration** — building the Stride project (call MSBuild/`dotnet` and stream log output)
- **Crash report helper** — `CrashReportHelper`/`StrideDebugService` not ported
- **Assembly Recompiler** — watching for DLL changes and reloading editor scripts
- **Remote facilities** — ServiceWire-based communication with the game process
- **Debug host** — embedded debugging support

---

## 4. Gaps Summary

### Architecture gaps

| Gap | Severity | Notes |
|---|---|---|
| File-picker dialog interfaces live in `Stride.Core.Presentation.Wpf/Services` | Medium | `IFileOpenModalDialog`, `IFileSaveModalDialog`, `IFolderOpenModalDialog` should be extracted to an agnostic location |
| Agnostic node-graph model has no dedicated project | Medium | Currently only the WPF graph controls exist in `Stride.Core.Presentation.Wpf.Graph` |
| Plugin system uses direct project references | Medium | Noted as known — dynamic loading is a future goal |

### Feature gaps (Avalonia vs WPF)

| Feature area | WPF loc estimate | Avalonia status |
|---|---|---|
| Docking layout manager | ~10 files | ❌ None — deferred |
| Asset editor views (all types) | ~279 files | ❌ All stubs |
| Gizmos system | ~43 files | ❌ Not started |
| Game editor controller | ~37 files | ❌ Not started |
| Curve/animation editor | ~35 files | ❌ Not started |
| Script/code editor (Roslyn) | ~15 files | ❌ Not started |
| Entity factories | ~22 files | ❌ Not started |
| Templates & wizards | ~252 files | ❌ Not started |
| Build integration + live log | ~5 files | ❌ Not started |
| Assembly recompiler / hot-reload | ~4 files | ❌ Not started |
| Asset picker dialog | ~3 files | ❌ Not started |
| Notification / progress windows | ~6 files | 🟡 Progress window exists |
| Node graph editor (visual script / graphics compositor) | ~21 files | ❌ Not started |
| FilteringComboBox | 2 files | ❌ Not ported |
| ColorPicker | 1 file | ❌ Not ported |
| Custom TreeView (virtualized) | ~5 files | ❌ Not ported |
| Drag-and-drop behaviors | ~15 files | ❌ Not ported |
| Crash report | ~7 files | ❌ Not ported |

---

## 5. Roadmap

The roadmap is structured as successive PoC and MVP milestones. Each milestone is independently usable and testable.

---

### Phase 0 — Foundation ✅ (Done)

**Goal:** Establish the 3-tier architecture and basic infrastructure.

**Achievements:**
- Extracted UI-agnostic ViewModels from WPF projects into new agnostic projects
- Created `Stride.Core.Presentation.Avalonia` with core converters, editors, controls, services
- Created `Stride.GameStudio.Avalonia` skeleton with menu, fixed layout, session loading
- Set up CI for Avalonia editor builds
- Launcher replaced with Avalonia version

---

### Phase 1 — PoC: Browse & Inspect 🟡 (In Progress)

**Goal:** A usable read-only project browser. Open a Stride project, explore its assets, inspect properties.

**Already done:**
- Open/close session
- Solution explorer (package/project/folder tree)
- Asset collection view with filters and display options
- Property grid for selected assets (with Quantum template providers)
- Undo/redo surfaced in the menu and action history panel
- Asset references panel

**Remaining for this phase:**
- [ ] Asset thumbnails displayed in the asset browser
- [ ] Complete `FilteringComboBox` / `SearchComboBox` for asset type filtering
- [ ] Extract file-picker interfaces (`IFileOpenModalDialog`, `IFileSaveModalDialog`, `IFolderOpenModalDialog`) from `Stride.Core.Presentation.Wpf/Services` to an agnostic location and provide an Avalonia implementation
- [ ] Asset picker dialog — basic modal for selecting an asset by type
- [ ] Drag-and-drop from asset browser to property fields (content references)
- [ ] Build-on-open (auto asset compilation) with progress reporting
- [ ] Notification window for background operations

---

### Phase 2 — PoC: Asset Preview 🔴 (Not Started)

**Goal:** Render live previews of assets (textures, models, materials, sounds) inside the editor.

**Requires:**
- [ ] Full `Stride.Editor.Avalonia` integration — wire `GameEngineHost` to a `PreviewGame` instance
- [ ] `GameStudioPreviewService` fully connected to `StridePreviewView`
- [ ] Asset preview views: `TexturePreviewView`, `ModelPreviewView`, `MaterialPreviewView`, `EntityPreviewView`, `SoundPreviewView`, `SkyboxPreviewView`, `SpriteFontPreviewView`, `HeightmapPreviewView`
- [ ] Thumbnail generation pipeline wired to asset browser

---

### Phase 3 — MVP 1: Scene Editing 🔴 (Not Started)

**Goal:** Open a scene, view and modify its entity hierarchy, edit entity/component properties, save changes.

**Requires:**
- [ ] `EntityHierarchyEditorView` — real implementation with embedded game viewport (extends Phase 2 work)
- [ ] `EditorGameController` equivalent — manages the in-editor `Game` instance lifecycle
- [ ] `EditorGameCameraService` — navigate/orbit the editor camera
- [ ] `EditorGameGridService` — floor grid rendering
- [ ] Basic gizmos — transform gizmo (translate, rotate, scale axes)
- [ ] Entity factories — add entities and components from within the editor
- [ ] `PrefabEditorView` — reuse entity hierarchy editor infrastructure
- [ ] Scene save/load round-trip verification

---

### Phase 4 — MVP 2: Full Asset Editors 🔴 (Not Started)

**Goal:** All major asset types can be fully edited, not just inspected.

**Requires:**
- [ ] **Script editor** — integrate an Avalonia text editor (e.g., `AvaloniaEdit`) for `ScriptSourceFileEditorView`; add Roslyn-based completion and diagnostics
- [ ] **Sprite Sheet editor** — pixel-accurate sprite frame editor with frame selection, UV display
- [ ] **UI Page / UI Library editor** — drag-resize adorners, canvas editing
- [ ] **Graphics Compositor editor** — node graph view (requires `Stride.Core.Presentation.Graph` Avalonia implementation)
- [ ] **Visual Script editor** — node graph view (shares graph infrastructure)
- [ ] `AssetPickerWindow` — fully featured modal dialog
- [ ] Template dialogs — item template wizard, `Add Asset` dialog

---

### Phase 5 — Feature Parity 🔴 (Long-term)

**Goal:** Achieve full parity with the WPF GameStudio. No blockers for everyday Stride development.

**Requires:**
- [ ] **Docking layout** — integrate an Avalonia docking library (e.g., `Dock.Avalonia` / `AvaloniaEdit DockManager`, or custom) to replace the hard-coded `GridSplitter` layout
- [ ] **Curve / animation editor** — key-frame timeline, easing, tangent handles
- [ ] **Full gizmos** — audio, camera, background, light, physics gizmos
- [ ] **Assembly recompiler** — watch for script DLL changes and hot-reload in the editor
- [ ] **Crash report** — Avalonia-side crash reporting dialog and log submission
- [ ] **Build integration** — full MSBuild-based project build, error list, output log
- [ ] **Plugin system** — dynamic assembly loading so Stride-specific plugins (and 3rd-party ones) are not hard-referenced
- [ ] **Translation/localization UI** — populate the `Stride.Core.Translation.Presentation` placeholder with Avalonia helpers
- [ ] **Cross-platform polish** — test and fix Linux/macOS-specific rendering, keyboard shortcuts, file dialogs
- [ ] **Performance & accessibility** — virtualized tree/tile panels, keyboard navigation, high-DPI support

---

## 6. Notes on Specific Design Decisions

### Why keep the WPF projects?

The WPF projects (`*.Wpf`) are kept for two reasons:
1. Maintaining a working editor for current users while the Avalonia port matures.
2. Serving as the authoritative **feature reference** when implementing Avalonia equivalents.

### Docking

There is no mature, production-ready Avalonia docking library comparable to AvalonDock. Options to watch or evaluate:
- [`Dock.Avalonia`](https://github.com/wieslawsoltes/Dock) — active project, used in other Avalonia-based editors.
- Custom implementation — high cost, but allows full control over persistence and UX.

This is explicitly deferred until after MVP 1 (Phase 3).

### Node graph editor

Both the Graphics Compositor and the Visual Script editors require a node-graph view. The WPF implementation is in `Stride.Core.Presentation.Wpf.Graph` (based on GraphX). An Avalonia equivalent will need to be identified or built. It would be worth creating a new agnostic project (e.g., `Stride.Core.Presentation.Graph`) to host the graph model so that the view implementation can be swapped independently of it.

### Script editor

The WPF script editor uses `RoslynPad.Editor.Windows` which is WPF-only. The Avalonia equivalent should use [`AvaloniaEdit`](https://github.com/AvaloniaUI/AvaloniaEdit) for the text editor control, and a Roslyn-based completion provider that does not depend on the WPF `RoslynPad.Roslyn.Windows` package.
