# Builds the donut test model: a torus with a three-bone rig, four shape keys and one animation clip that drives both,
# then exports donut.fbx and donut.gltf next to this script. Run headless:
#   blender --background --factory-startup --python make_donut.py
import math
import os

import bpy
from mathutils import Vector

OUT = os.path.dirname(os.path.abspath(__file__))
MAJOR, MINOR = 1.0, 0.35
# The clip runs from frame 0 to FRAMES, so its first key is at 0 s in both the FBX and the glTF export.
FRAMES = 60

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.frame_start, scene.frame_end = 0, FRAMES
scene.render.fps = 30

# Mesh
bpy.ops.mesh.primitive_torus_add(major_radius=MAJOR, minor_radius=MINOR, major_segments=48, minor_segments=24)
donut = bpy.context.active_object
donut.name = donut.data.name = "Donut"
bpy.ops.object.shade_smooth()
mesh = donut.data

# Shape keys: each one is a function of the base position.
def ring_center(co):
    # Closest point on the ring the tube follows.
    flat = Vector((co.x, co.y, 0.0))
    return flat.normalized() * MAJOR if flat.length > 1e-6 else Vector((MAJOR, 0.0, 0.0))

shapes = {
    "Bigger": lambda co: co * 1.3,
    "Thicker": lambda co: ring_center(co) + (co - ring_center(co)) * 1.6,
    "Wider": lambda co: Vector((co.x * 1.5, co.y, co.z)),
    "Flatter": lambda co: Vector((co.x, co.y, co.z * 0.4)),
}
donut.shape_key_add(name="Basis", from_mix=False)
for name, shape in shapes.items():
    key = donut.shape_key_add(name=name, from_mix=False)
    for vertex, point in zip(mesh.vertices, key.data):
        point.co = shape(vertex.co)

# Rig: Root at the center, Left and Right bones on each side of the ring.
bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
rig = bpy.context.active_object
rig.name = rig.data.name = "DonutRig"
bones = rig.data.edit_bones
root = bones[0]
root.name = "Root"
root.head, root.tail = (0, 0, 0), (0, 0, 0.5)
for name, side in (("Left", -1), ("Right", 1)):
    bone = bones.new(name)
    bone.head, bone.tail = (side * 0.3, 0, 0), (side * MAJOR, 0, 0)
    bone.parent = root
bpy.ops.object.mode_set(mode="OBJECT")

# Skin: smooth blend between Left and Right across the ring, Root keeps a share near the middle.
groups = {name: donut.vertex_groups.new(name=name) for name in ("Root", "Left", "Right")}
for vertex in mesh.vertices:
    side = max(-1.0, min(1.0, vertex.co.x / MAJOR))
    right = 0.5 + 0.5 * side
    weights = {"Left": 1.0 - right, "Right": right}
    weights["Root"] = 0.2 * (1.0 - abs(side))
    total = sum(weights.values())
    for name, weight in weights.items():
        if weight > 0:
            groups[name].add([vertex.index], weight / total, "REPLACE")
donut.parent = rig
modifier = donut.modifiers.new("Armature", "ARMATURE")
modifier.object = rig

# Animation "Wobble": shape keys rise and fall one after another, the side bones lift and drop, the root sways.
# Bone rotations are in bone space, where Y runs along the bone: the root (pointing up) spins around its Y, the side
# bones (pointing outward) bend the ring up and down around their X. The clip ends where it starts, so it loops.
keys = mesh.shape_keys.key_blocks
step = FRAMES // len(shapes)
for index, name in enumerate(shapes):
    block = keys[name]
    start = index * step
    for frame, value in ((0, 0.0), (start, 0.0), (start + step // 2, 1.0), (start + step, 0.0), (FRAMES, 0.0)):
        block.value = value
        block.keyframe_insert("value", frame=frame)
mesh.shape_keys.animation_data.action.name = "WobbleShapes"

bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="POSE")
pose = rig.pose.bones
for frame, swing, sway in ((0, 0.0, 0.0), (FRAMES // 4, 0.4, 1.0), (FRAMES // 2, 0.0, 0.0),
                           (3 * FRAMES // 4, -0.4, -1.0), (FRAMES, 0.0, 0.0)):
    pose["Left"].rotation_mode = pose["Right"].rotation_mode = pose["Root"].rotation_mode = "XYZ"
    pose["Left"].rotation_euler = (swing, 0, 0)
    pose["Right"].rotation_euler = (swing, 0, 0)
    pose["Root"].rotation_euler = (0, sway * math.radians(20), 0)
    for bone in ("Left", "Right", "Root"):
        pose[bone].keyframe_insert("rotation_euler", frame=frame)
bpy.ops.object.mode_set(mode="OBJECT")
rig.animation_data.action.name = "Wobble"

# Every key must lie in the scene range: the FBX export bakes the scene range, the glTF export each action's range.
for action in bpy.data.actions:
    first, last = action.frame_range
    if first < scene.frame_start or last > scene.frame_end:
        raise RuntimeError(f"Action {action.name} spans frames {first}-{last}, outside {scene.frame_start}-{scene.frame_end}.")

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT, "donut.fbx"),
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=False,
    use_mesh_modifiers=False,
)
bpy.ops.export_scene.gltf(
    filepath=os.path.join(OUT, "donut.gltf"),
    export_format="GLTF_SEPARATE",
    use_selection=True,
    export_animations=True,
    export_morph=True,
    export_skins=True,
    export_animation_mode="ACTIVE_ACTIONS",
)
print("DONUT_EXPORT_OK")
