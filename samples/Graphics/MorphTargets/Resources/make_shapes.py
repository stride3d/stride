# Builds the unskinned morph test models: a cube and an icosphere, each with four shape keys, no armature and one
# clip that drives the shape keys, then exports Cube/cube.gltf and Icosphere/icosphere.gltf next to this script. Run headless:
#   blender --background --factory-startup --python make_shapes.py
import math
import os

import bpy
from mathutils import Vector

OUT = os.path.dirname(os.path.abspath(__file__))


FRAMES = 60


def build(name, create_mesh, shapes, clips):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    create_mesh()
    model = bpy.context.active_object
    model.name = model.data.name = name
    model.shape_key_add(name="Basis", from_mix=False)
    for shape_name, shape in shapes.items():
        key = model.shape_key_add(name=shape_name, from_mix=False)
        for vertex, point in zip(model.data.vertices, key.data):
            point.co = shape(vertex.co.copy())
    # Each clip keys (frame, {shape: weight}); its first and last keys match, so it loops. The clip is stashed as an NLA
    # track so the glTF export writes it under its name.
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 0, FRAMES
    bpy.context.scene.render.fps = 30
    shape_keys = model.data.shape_keys
    shape_keys.animation_data_create()
    for clip_name, keys in clips.items():
        action = bpy.data.actions.new(clip_name)
        shape_keys.animation_data.action = action
        for frame, weights in keys:
            for shape_name in shapes:
                block = shape_keys.key_blocks[shape_name]
                block.value = weights.get(shape_name, 0.0)
                block.keyframe_insert("value", frame=frame)
        track = shape_keys.animation_data.nla_tracks.new()
        track.name = clip_name
        track.strips.new(clip_name, 0, action)
        shape_keys.animation_data.action = None
    for block in shape_keys.key_blocks:
        block.value = 0.0
    folder = os.path.join(OUT, name)
    os.makedirs(folder, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(
        filepath=os.path.join(folder, name.lower() + ".gltf"),
        export_format="GLTF_SEPARATE",
        use_selection=True,
        export_animations=True,
        export_animation_mode="NLA_TRACKS",
        export_morph=True,
        export_skins=False,
    )


def subdivided_cube():
    bpy.ops.mesh.primitive_cube_add(size=1.6)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.subdivide(number_cuts=8)
    bpy.ops.object.mode_set(mode="OBJECT")


def twist(co):
    angle = co.z * math.radians(60)
    cosine, sine = math.cos(angle), math.sin(angle)
    return Vector((co.x * cosine - co.y * sine, co.x * sine + co.y * cosine, co.z))


build("Cube", subdivided_cube, {
    "Taller": lambda co: Vector((co.x, co.y, co.z * 1.6)),
    "Twisted": twist,
    "Tapered": lambda co: Vector((co.x * (1.0 - 0.35 * co.z), co.y * (1.0 - 0.35 * co.z), co.z)),
    "Rounded": lambda co: co.normalized() * 0.8 * math.sqrt(3) * 0.75,
}, {
    "Morph": [(0, {}), (12, {"Taller": 1.0}), (24, {"Twisted": 1.0}), (36, {"Tapered": 1.0}), (48, {"Rounded": 1.0}), (FRAMES, {})],
})


def icosphere():
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=4, radius=1.0)
    bpy.ops.object.shade_smooth()


build("Icosphere", icosphere, {
    "Squashed": lambda co: Vector((co.x * 1.3, co.y * 1.3, co.z * 0.5)),
    "Stretched": lambda co: Vector((co.x * 0.7, co.y * 0.7, co.z * 1.6)),
    "Bumpy": lambda co: co * (1.0 + 0.15 * math.sin(co.x * 8) * math.sin(co.y * 8) * math.sin(co.z * 8)),
    "Pear": lambda co: Vector((co.x * (1.0 - 0.3 * co.z), co.y * (1.0 - 0.3 * co.z), co.z)),
}, {
    "Morph": [(0, {}), (12, {"Squashed": 1.0}), (24, {"Stretched": 1.0}), (36, {"Bumpy": 1.0}), (48, {"Pear": 1.0}), (FRAMES, {})],
})
print("SHAPES_EXPORT_OK")
