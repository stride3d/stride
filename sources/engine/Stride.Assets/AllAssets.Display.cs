// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets
{
    /// <summary>
    /// Define the display priority for groups of assets.
    /// </summary>
    /// <remarks>
    /// Higher priority are displayed first.
    /// </remarks>
    public enum AssetDisplayPriority
    {
        Default = 0,
        Effect = 100,
        Scripts = 500,
        Physics = 800,
        Navigation = 900,
        Skyboxes = 1000,
        Media = 1100,
        UI = 1200,
        Fonts = 1300,
        Sprites = 1400,
        Textures = 1500,
        Materials = 1600,
        Models = 1800,
        Entities = 1900,
        GraphicsCompositor = 5000,
        GameSettings = int.MaxValue, // Game settings should always be displayed first
    }

    [Display((int)AssetDisplayPriority.GameSettings, "Game settings")]
    partial class GameSettingsAsset
    {
    }

    namespace Effect
    {
        [Display((int)AssetDisplayPriority.Effect, "Effect library")]
        partial class EffectLogAsset
        {
        }

        [Display((int)AssetDisplayPriority.Effect + 25, "Effect shader")]
        partial class EffectShaderAsset
        {
        }

        [Display((int)AssetDisplayPriority.Effect + 50, "Effect compositor")]
        partial class EffectCompositorAsset
        {
        }
    }

    namespace Entities
    {
        [Display((int)AssetDisplayPriority.Entities, "Prefab")]
        partial class PrefabAsset
        {
        }

        [Display((int)AssetDisplayPriority.Entities + 50, "Scene")]
        partial class SceneAsset
        {
        }
    }

    namespace Materials
    {
        [Display((int)AssetDisplayPriority.Materials, "Material")]
        partial class MaterialAsset
        {
        }
    }

    namespace Media
    {
        [Display((int)AssetDisplayPriority.Media, "Sound")]
        partial class SoundAsset
        {
        }

        [Display((int)AssetDisplayPriority.Media + 50, "Video")]
        partial class VideoAsset
        {
        }
    }

    namespace Navigation
    {
        [Display((int)AssetDisplayPriority.Navigation, "Navigation mesh")]
        partial class NavigationMeshAsset
        {
        }
    }

    namespace Physics
    {
        [Display((int)AssetDisplayPriority.Physics, "Collider shape")]
        partial class ColliderShapeAsset
        {
        }

        [Display((int)AssetDisplayPriority.Physics + 50, "Heightmap")]
        partial class HeightmapAsset
        {
        }
    }

    namespace Rendering
    {
        [Display((int)AssetDisplayPriority.GraphicsCompositor, "Graphics compositor")]
        partial class GraphicsCompositorAsset
        {
        }
    }

    namespace Scripts
    {
        [Display((int)AssetDisplayPriority.Scripts, "Script Source Code")]
        partial class ScriptSourceFileAsset
        {
        }

        [Display((int)AssetDisplayPriority.Scripts + 50, "Visual Script")]
        partial class VisualScriptAsset
        {
        }
    }

    namespace Skyboxes
    {
        [Display((int)AssetDisplayPriority.Skyboxes, "Skybox")]
        partial class SkyboxAsset
        {
        }
    }

    namespace Sprite
    {
        [Display((int)AssetDisplayPriority.Sprites, "Sprite sheet")]
        partial class SpriteSheetAsset
        {
        }
    }

    namespace SpriteFont
    {
        [Display((int)AssetDisplayPriority.Fonts, "Sprite font")]
        partial class SpriteFontAsset
        {
        }

        [Display((int)AssetDisplayPriority.Fonts + 50, "Sprite font (precompiled)")]
        partial class PrecompiledSpriteFontAsset
        {
        }
    }

    namespace Textures
    {
        [Display((int)AssetDisplayPriority.Textures, "Texture")]
        partial class TextureAsset
        {
        }

        [Display((int)AssetDisplayPriority.Textures + 50, "Render texture")]
        partial class RenderTextureAsset
        {
        }
    }

    namespace UI
    {
        [Display((int)AssetDisplayPriority.UI, "UI page")]
        partial class UIPageAsset
        {
        }

        [Display((int)AssetDisplayPriority.UI + 50, "UI library")]
        partial class UILibraryAsset
        {
        }
    }
}
