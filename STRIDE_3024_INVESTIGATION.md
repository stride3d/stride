# Investigation: Stride #3024 - Parent Scale Affecting Children

## Problem Description

In Ad Astra Imperium (production Stride game), a planet was appearing **larger than its parent star** after scene reload. This matches Stride issue #3024 where collision/entity size is affected by parent object scale inconsistently.

## Evidence from Ad Astra Imperium

**Bug Report** (GitHub Issue #188):
- "Starport is huge for some reason and on top of the star"
- "Phased through a wall on planets"
- "Phased through planet instead of bouncing"

## Database Values (Lambda Kappa System)

**Star (parent entity):**
```json
{
  "_id": "Lambda Kappa",
  "position": {"x": 0.0, "y": 80.0, "z": 0.0},
  "scale": {"x": 20.0, "y": 20.0, "z": 20.0},
  "typeString": "Indigo Dwarf"
}
```

**Planet (child of star):**
```json
{
  "_id": "Lambda Kappa I",
  "position": {"x": -1.977601, "y": 0.0, "z": 0.2738852},
  "scale": {"x": 0.3, "y": 0.3, "z": 0.3}
}
```

**Starport (child of star):**
```json
{
  "_id": "Lambda Kappa",
  "position": {"x": 3.5, "y": 0.0, "z": 0.0},
  "scale": {"x": 0.05, "y": 0.05, "z": 0.05},
  "typeString": "Starport"
}
```

**Comparison with Sol (template system):**

| Entity | Scale | Expected Ratio |
|--------|-------|----------------|
| Sol (star) | 30 | - |
| Earth | 0.3 | Star 100x bigger |
| Lambda Kappa (star) | 20 | - |
| Lambda Kappa I | 0.3 | Star ~67x bigger |

## Code That Creates Parent-Child Relationship

**File:** `AdAstraImperium.Server/Managers/StarSystemManager.cs` (lines 231-244)

```csharp
if (starInfo.hasStarbase || starInfo.hasStarport)
{
    StarportInfoDB starportInfo = ServerGlobalCache.GetStarportByName(serverScene.Name);
    Entity starportEntity = new Entity($"{serverScene.Name} {starportInfo.typeString}",
        starportInfo.Position, starportInfo.Rotation, starportInfo.Scale) { };

    // ... setup ...

    serverScene.AddEntity(starportEntity, star);  // <-- PARENTED TO STAR
}
```

Planets are also parented to the star (line 293):
```csharp
serverScene.AddEntity(entity, star);  // planet parented to star
```

## Observed Behavior

**Expected:**
- Star displays at scale 20
- Planet displays at local scale 0.3 (world scale = 20 × 0.3 = 6, relative to star)
- Star should be ~3x visually larger than planet in world space

**Actual (bug):**
- Star appears at incorrect scale (possibly 1)
- Planet appears at scale 6 (parent scale applied)
- Planet appears **larger than the star**

## Hypothesis

The parent scale is being applied to children but **not to the parent itself** on reload, OR the parent scale is being applied **twice** to children.

## Stride Code to Investigate

1. `TransformComponent` - how scale inheritance works
2. Entity serialization/deserialization - scale handling on reload
3. Scene loading - when parent-child relationships are restored
4. Prefab instantiation - if prefabs handle scale differently

## Related Stride Issues

- **#3024** - Collision size is affected by the parent object scale
- **#3018** - Static Mesh Collision detection issues (some faces not detecting)

---

## Diagnosis Progress

### Key Code Paths Identified

**1. TransformComponent.cs (line 317)**
```csharp
Matrix.Multiply(ref LocalMatrix, ref Parent.WorldMatrix, out WorldMatrix);
```
- Child's WorldMatrix = LocalMatrix × Parent.WorldMatrix
- This correctly includes parent scale in child's world transform

**2. PhysicsComponent.cs (lines 461-486)**
```csharp
internal void DerivePhysicsTransformation(out Matrix derivedTransformation, bool forceUpdateTransform = true)
{
    if (BoneIndex == -1)
    {
        if (forceUpdateTransform)
            Entity.Transform.UpdateWorldMatrix();
        derivedTransformation = Entity.Transform.WorldMatrix;  // <-- Includes parent scale
    }
    // ...
    derivedTransformation.Decompose(out Vector3 scale, out Matrix rotation, out Vector3 translation);

    if (CanScaleShape)
    {
        if (ColliderShape.Scaling != scale)
        {
            ColliderShape.Scaling = scale;  // <-- Scale extracted from WorldMatrix applied to collider
        }
    }
}
```

**3. CanScaleShape behavior (lines 609-614)**
```csharp
CanScaleShape = true;
foreach (var desc in ColliderShapes)
{
    if(desc is ColliderShapeAssetDesc)
        CanScaleShape = false;  // <-- Asset-based shapes don't get dynamic scaling
}
```

### Current Hypothesis

The bug likely occurs during **deserialization/loading order**:

1. On reload, entities are deserialized
2. Physics components call `Entity.Transform.UpdateWorldMatrix()` during attach
3. But parent-child transform relationships may not be fully established yet
4. OR the collider shape SIZE is being saved with parent scale baked in

The fact that "it goes back to normal after changing a value and changing it back" suggests the runtime recalculation is correct, but the initial load state is wrong.

### Areas Needing Further Investigation

1. **Scene/Entity deserialization order** - When are parent-child relationships established relative to physics component attachment?
2. **Editor save process** - Is the collider shape Size being saved with world scale instead of local?
3. **Prefab instantiation** - Different code path for prefabs vs scene entities?

### ColliderShape.Scaling vs Size

- `ColliderShape.Scaling` - Applied dynamically from entity WorldMatrix (line 484)
- Shape description `Size` - Static value saved in asset (BoxColliderShapeDesc.Size)

If `Size` is correct but `Scaling` is being applied incorrectly on load, that would cause the bug.

---

## Next Steps

1. Add debug logging to track loading order
2. Check if prefab instantiation has different scale handling
3. Look at editor save code for collider shapes
4. Test with minimal repro case
