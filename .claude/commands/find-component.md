---
name: find-component
description: Find and explain EntityComponent implementations in the Stride ECS
---

# Find Component Command

Find and explain EntityComponent implementations in the Stride ECS.

## Usage

```
/find-component <component-name>
```

## Instructions

Search for EntityComponent implementations in the codebase.

### Search Strategy

1. Search for the component class:
```
grep -r "class.*Component.*:.*EntityComponent" sources/engine/
```

2. Common component locations:
   - `sources/engine/Stride.Engine/Engine/` - Core components
   - `sources/engine/Stride.Audio/` - Audio components
   - `sources/engine/Stride.Physics/` - Physics components
   - `sources/engine/Stride.BepuPhysics/` - Bepu physics components
   - `sources/engine/Stride.Navigation/` - Navigation components
   - `sources/engine/Stride.Particles/` - Particle components
   - `sources/engine/Stride.UI/` - UI components

3. Also search for the associated EntityProcessor:
```
grep -r "class.*Processor.*:.*EntityProcessor" sources/engine/
```

### Key Components to Know

- `TransformComponent` - Position, rotation, scale (every entity has one)
- `ModelComponent` - 3D model rendering
- `CameraComponent` - Camera viewpoint
- `LightComponent` - Light sources
- `ScriptComponent` - User scripts (SyncScript, AsyncScript)
- `AudioEmitterComponent` - Sound sources
- `RigidbodyComponent` / `StaticColliderComponent` - Physics
- `CharacterComponent` - Character controller
- `NavigationComponent` - AI pathfinding
- `SpriteComponent` - 2D sprites
- `UIComponent` - UI elements in 3D space

Report the component's purpose, key properties, and its associated processor if any.
