# Analyze Asset Command

Analyze a Stride asset file (.sd* files) and explain its structure.

## Usage

```
/analyze-asset <asset-path>
```

## Instructions

Stride assets are YAML-based files with specific extensions:
- `.sdscene` - Scene files
- `.sdmat` - Material files
- `.sdprefab` - Prefab files
- `.sdmodel` - Model import settings
- `.sdtex` - Texture import settings
- `.sdfnt` - Font files
- `.sdfx` - Effect/shader files
- `.sdsprite` - Sprite sheet files
- `.sdanim` - Animation files

### Analysis Steps

1. Read the asset file (it's YAML format)
2. Identify the asset type from the `!` type tag at the root
3. Explain the key properties and their purpose
4. List any referenced assets (other files it depends on)
5. Identify any potential issues or optimizations

### Common Asset Types

- `MaterialAsset` - Material definitions with shader parameters
- `SceneAsset` - Scene hierarchy with entities and components
- `PrefabAsset` - Reusable entity templates
- `TextureAsset` - Texture import and compression settings
- `ModelAsset` - 3D model import settings
- `SpriteSheetAsset` - 2D sprite definitions

### Asset Locations

Assets are typically found in:
- `samples/` - Sample project assets
- `sources/editor/Stride.Assets.Presentation/Templates/` - Template assets
- Test projects under `sources/engine/*/Tests/`

Report the asset type, key configuration, dependencies, and any notable settings.
