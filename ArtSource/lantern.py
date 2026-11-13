"""
Sun Switch: the Lightkeeper's lantern, for Blender (5.x).

A small hexagonal storybook lantern that hangs on the Lightkeeper's back. Two objects:
  - Lantern_Frame: hanging ring, cap, six posts and base (brass and iron vertex colours)
  - Lantern_Glow:  the glass core; Unity tints its emission gold (Day) or moon blue (Night)

Blender is Z-up; the FBX export converts to Unity's Y-up. The pivot is the hanging point at
the top of the ring, so the prefab can hang it from a hook on the back.

Run inside Blender (Scripting tab, the Blender MCP, or the command line):
    exec(open("<repo>/ArtSource/lantern.py").read())
    build()
    export("<repo>/Assets/Art/Characters/Lightkeeper")
or: blender -b --factory-startup --python ArtSource/lantern.py -- <export folder>
"""

import math
import os
import sys

import bmesh
import bpy

COLLECTION = "SunSwitch_Lantern"
SIDES = 6

# Sizes are in the KayKit model's units (the model is ~2.2 m tall before the prefab scales it).
RADIUS = 0.145         # cap / base radius
GLASS_RADIUS = 0.112   # glass core radius
RING_RADIUS = 0.048    # hanging ring
RING_THICKNESS = 0.013
CAP_HEIGHT = 0.12
BODY_HEIGHT = 0.255    # between cap and base
BASE_HEIGHT = 0.06
POST_SIZE = 0.024
BAND = 0.02            # iron bands under the cap and over the base

# Vertex colours as sRGB, multiplied with the white Storybook material in Unity. They are
# exported as linear values because the project renders in Linear colour space and the
# shader reads vertex colours raw.
COLORS = {
    "brass": (0.80, 0.58, 0.32, 1.0),
    "iron": (0.32, 0.27, 0.25, 1.0),
    "glass": (1.0, 1.0, 1.0, 1.0),
}


def _collection():
    col = bpy.data.collections.get(COLLECTION)
    if col is None:
        col = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(col)
    for obj in list(col.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    return col


def _to_linear(c):
    return c if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _paint(bm, faces, color):
    layer = bm.loops.layers.color.get("Col") or bm.loops.layers.color.new("Col")
    color = tuple(_to_linear(c) for c in color[:3]) + (color[3],)
    for f in faces:
        for loop in f.loops:
            loop[layer] = color


def _prism(bm, radius_bottom, radius_top, z0, z1, sides=SIDES, rotation=0.0):
    """Closed n-sided prism / frustum between z0 and z1. Returns its faces."""
    bottom, top = [], []
    for k in range(sides):
        a = rotation + 2 * math.pi * k / sides
        bottom.append(bm.verts.new((radius_bottom * math.cos(a), radius_bottom * math.sin(a), z0)))
        top.append(bm.verts.new((radius_top * math.cos(a), radius_top * math.sin(a), z1)))
    faces = [bm.faces.new(list(reversed(bottom))), bm.faces.new(top)]
    for k in range(sides):
        j = (k + 1) % sides
        faces.append(bm.faces.new((bottom[k], bottom[j], top[j], top[k])))
    return faces


def _box(bm, cx, cy, z0, z1, size):
    h = size / 2
    return _prism(bm, h * math.sqrt(2), h * math.sqrt(2), z0, z1, sides=4, rotation=math.pi / 4 + math.atan2(cy, cx))


def _ring(bm, z_centre):
    """Hanging ring standing upright in the XZ plane."""
    faces = []
    segs, tube = 12, 6
    rows = []
    for i in range(segs):
        u = 2 * math.pi * i / segs
        row = []
        for j in range(tube):
            v = 2 * math.pi * j / tube
            r = RING_RADIUS + RING_THICKNESS * math.cos(v)
            row.append(bm.verts.new((r * math.cos(u), RING_THICKNESS * math.sin(v), z_centre + r * math.sin(u))))
        rows.append(row)
    for i in range(segs):
        for j in range(tube):
            a, b = rows[i][j], rows[(i + 1) % segs][j]
            c, d = rows[(i + 1) % segs][(j + 1) % tube], rows[i][(j + 1) % tube]
            faces.append(bm.faces.new((a, b, c, d)))
    return faces


def _object(name, bm, col):
    mesh = bpy.data.meshes.new(name)
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth = False
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    return obj


def build():
    col = _collection()

    # Heights, top (pivot, z = 0) to bottom.
    ring_centre = -RING_RADIUS - RING_THICKNESS
    cap_top = ring_centre - RING_RADIUS * 0.6
    cap_bottom = cap_top - CAP_HEIGHT
    body_bottom = cap_bottom - BODY_HEIGHT
    base_bottom = body_bottom - BASE_HEIGHT

    # Frame.
    bm = bmesh.new()
    _paint(bm, _ring(bm, ring_centre), COLORS["iron"])
    _paint(bm, _prism(bm, RADIUS * 1.12, RADIUS * 0.25, cap_bottom, cap_top), COLORS["brass"])
    _paint(bm, _prism(bm, RADIUS * 1.18, RADIUS * 1.18, cap_bottom - BAND, cap_bottom), COLORS["iron"])
    for k in range(SIDES):
        a = 2 * math.pi * k / SIDES
        x, y = RADIUS * math.cos(a), RADIUS * math.sin(a)
        _paint(bm, _box(bm, x, y, body_bottom, cap_bottom - BAND, POST_SIZE), COLORS["iron"])
    _paint(bm, _prism(bm, RADIUS * 1.18, RADIUS * 1.18, body_bottom - BAND, body_bottom), COLORS["iron"])
    _paint(bm, _prism(bm, RADIUS * 0.7, RADIUS * 1.05, base_bottom, body_bottom - BAND), COLORS["brass"])
    frame = _object("Lantern_Frame", bm, col)

    # Glass core (rotated half a step so its edges sit between the posts).
    bm = bmesh.new()
    _paint(bm, _prism(bm, GLASS_RADIUS, GLASS_RADIUS, body_bottom, cap_bottom - BAND,
                      rotation=math.pi / SIDES), COLORS["glass"])
    glow = _object("Lantern_Glow", bm, col)
    return frame, glow


def export(folder):
    os.makedirs(folder, exist_ok=True)
    col = bpy.data.collections[COLLECTION]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = col.objects[0]
    path = os.path.join(folder, "Lantern.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        colors_type="LINEAR",
        add_leaf_bones=False,
        bake_anim=False,
    )
    print("Exported", path)
    return path


def _run_from_command_line():
    """True only for `blender --python .../lantern.py -- <folder>`, not when exec()'d."""
    argv = sys.argv
    return ("--python" in argv and argv[argv.index("--python") + 1].endswith("lantern.py")
            and "--" in argv)


if _run_from_command_line():
    out = sys.argv[sys.argv.index("--") + 1]
    build()
    export(out)
