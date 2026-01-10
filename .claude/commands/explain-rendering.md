---
name: explain-rendering
description: Explain how rendering features work (materials, shaders, post-processing)
---

# Explain Rendering Command

Explain how a specific rendering feature works in Stride.

## Usage

```
/explain-rendering <feature>
```

## Instructions

The Stride rendering system is in `sources/engine/Stride.Rendering/`.

### Key Rendering Concepts

1. **RenderFeature** - Modular rendering capabilities
   - Location: `Stride.Rendering/Rendering/`
   - Examples: `MeshRenderFeature`, `SpriteRenderFeature`, `ParticleEmitterRenderFeature`

2. **RenderStage** - Named rendering passes
   - Opaque, Transparent, Shadow, etc.
   - Configured in `GraphicsCompositor`

3. **RenderObject** - Items to render
   - Extracted from scene components
   - Processed by render features

4. **GraphicsCompositor** - Orchestrates rendering
   - Location: `Stride.Rendering/Rendering/Compositing/`
   - Defines render stages, cameras, and post-processing

5. **Effects/Shaders**
   - SDSL shader language (Stride Shading Language)
   - Location: `sources/engine/Stride.Rendering/Rendering/` (*.sdsl files)
   - Shader mixing system for composition

### Search Locations

- `sources/engine/Stride.Rendering/Rendering/` - Core rendering
- `sources/engine/Stride.Rendering/Rendering/Materials/` - Material system
- `sources/engine/Stride.Rendering/Rendering/Lights/` - Lighting
- `sources/engine/Stride.Rendering/Rendering/Shadows/` - Shadow mapping
- `sources/engine/Stride.Rendering/Rendering/Images/` - Post-processing

### Common Features to Explain

- Forward rendering vs deferred
- PBR materials
- Shadow mapping
- Post-processing effects
- Clustered lighting
- Subsurface scattering
- Screen-space reflections

Search for the feature, read the relevant code, and explain the data flow and key classes involved.
