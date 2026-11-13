"""
Sun Switch: obstacle meshes for Blender (5.x) — GDD §5 obstacle toolkit.

  Obstacle_KillBar_Post   short broken column the bar spins on       pivot: bottom centre
  Obstacle_KillBar_Bar    5.5 m striped beam + hub (rotates)         pivot: rotation centre
  Obstacle_JumpPad        2 x 2 m carved launch stone with rune disc pivot: bottom centre
  Obstacle_KillSurface_S  2 x 2 m bed of ink-thorn spikes            pivot: bottom centre
  Obstacle_KillSurface_M  4 x 4 m bed of ink-thorn spikes            pivot: bottom centre
  Obstacle_Shard          floating crystal pair, ~0.8 m (collectible) pivot: centre

Obstacles sit ON a platform, so they pivot at the bottom (place them at the platform's top y).
Blender is Z-up; the FBX export converts to Unity's Y-up.

Vertex colours are raw tint multipliers (exported as linear, read as-is by the Storybook
shader) under the phase material colour. Hazard parts are a darker red-brown so they read as
dangerous in gold, indigo and grey alike.

Run inside Blender (Scripting tab, the Blender MCP, or the command line):
    exec(open("<repo>/ArtSource/obstacles.py").read())
    build_all()
    export_all("<repo>/Assets/Art/Meshes/Obstacles")
or: blender -b --factory-startup --python ArtSource/obstacles.py -- <export folder>
"""

import math
import os
import sys

import bmesh
import bpy

COLLECTION = "SunSwitch_Obstacles"

TINT = {
    "stone": (0.92, 0.92, 0.92, 1.0),
    "stone_dark": (0.72, 0.70, 0.74, 1.0),
    "danger": (0.86, 0.52, 0.46, 1.0),
    "danger_dark": (0.62, 0.38, 0.38, 1.0),
    "rune": (1.0, 1.0, 1.0, 1.0),
    "crystal": (1.0, 1.0, 1.0, 1.0),
}

KILLBAR_LENGTH = 5.5      # full bar length (m); sweeps a 2.75 m radius
KILLBAR_THICK = 0.26
KILLBAR_HEIGHT = 0.75     # bar centre above the post's bottom (set in the prefab)
JUMPPAD_SIZE = 2.0
JUMPPAD_HEIGHT = 0.15
SPIKE_SPACING = 0.5
SPIKE_HEIGHT = 0.28
SPIKE_BASE = 0.34
KILL_BASE_HEIGHT = 0.1


# --------------------------------------------------------------------- bmesh helpers

def _paint(bm, faces, tint):
    layer = bm.loops.layers.color.get("Col") or bm.loops.layers.color.new("Col")
    for f in faces:
        for loop in f.loops:
            loop[layer] = TINT[tint]


def _prism(bm, poly, z0, z1, top_scale=1.0):
    """Closed prism over a CCW 2D polygon from z0 to z1 (top optionally scaled)."""
    bottom = [bm.verts.new((x, y, z0)) for x, y in poly]
    top = [bm.verts.new((x * top_scale, y * top_scale, z1)) for x, y in poly]
    faces = [bm.faces.new(list(reversed(bottom))), bm.faces.new(top)]
    n = len(poly)
    for k in range(n):
        j = (k + 1) % n
        faces.append(bm.faces.new((bottom[k], bottom[j], top[j], top[k])))
    return faces


def _box(bm, x0, x1, y0, y1, z0, z1):
    return _prism(bm, [(x0, y0), (x1, y0), (x1, y1), (x0, y1)], z0, z1)


def _ngon(radius, sides, phase=0.0):
    return [(radius * math.cos(phase + 2 * math.pi * k / sides),
             radius * math.sin(phase + 2 * math.pi * k / sides)) for k in range(sides)]


def _chamfered_square(size, cut):
    h = size / 2
    return [(-h + cut, -h), (h - cut, -h), (h, -h + cut), (h, h - cut),
            (h - cut, h), (-h + cut, h), (-h, h - cut), (-h, -h + cut)]


def _pyramid(bm, cx, cy, base, z0, height):
    h = base / 2
    corners = [bm.verts.new((cx + x, cy + y, z0)) for x, y in ((-h, -h), (h, -h), (h, h), (-h, h))]
    apex = bm.verts.new((cx, cy, z0 + height))
    faces = [bm.faces.new(list(reversed(corners)))]
    for k in range(4):
        faces.append(bm.faces.new((corners[k], corners[(k + 1) % 4], apex)))
    return faces


def _bipyramid(bm, cx, cy, cz, radius, up, down, sides=6, tilt=0.0):
    ring = []
    for x, y in _ngon(radius, sides):
        # tilt around the x axis
        yy, zz = y * math.cos(tilt), y * math.sin(tilt)
        ring.append(bm.verts.new((cx + x, cy + yy, cz + zz)))
    top = bm.verts.new((cx, cy - up * math.sin(tilt), cz + up * math.cos(tilt)))
    bottom = bm.verts.new((cx, cy + down * math.sin(tilt), cz - down * math.cos(tilt)))
    faces = []
    for k in range(sides):
        j = (k + 1) % sides
        faces.append(bm.faces.new((ring[k], ring[j], top)))
        faces.append(bm.faces.new((ring[j], ring[k], bottom)))
    return faces


def _object(name, bm, col):
    old = bpy.data.objects.get(name)
    if old is not None:
        mesh = old.data
        bpy.data.objects.remove(old, do_unlink=True)
        bpy.data.meshes.remove(mesh)
    mesh = bpy.data.meshes.new(name)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth = False
    col_attr = mesh.color_attributes.get("Col")
    if col_attr is not None:
        mesh.color_attributes.active_color = col_attr
        mesh.color_attributes.render_color_index = mesh.color_attributes.active_color_index
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    return obj


# --------------------------------------------------------------------- builders

def build_killbar_post(col):
    bm = bmesh.new()
    _paint(bm, _prism(bm, _ngon(0.45, 8, math.pi / 8), 0.0, 0.12), "stone_dark")
    _paint(bm, _prism(bm, _ngon(0.26, 8, math.pi / 8), 0.12, 0.56, top_scale=0.92), "stone")
    _paint(bm, _prism(bm, _ngon(0.32, 8, math.pi / 8), 0.56, 0.6), "stone_dark")
    return _object("Obstacle_KillBar_Post", bm, col)


def build_killbar_bar(col):
    bm = bmesh.new()
    t = KILLBAR_THICK / 2
    _paint(bm, _prism(bm, _ngon(0.32, 8, math.pi / 8), -0.17, 0.17), "stone_dark")
    # Beam segments with alternating danger stripes, from the hub outwards on both sides.
    half = KILLBAR_LENGTH / 2
    seg = 0.45
    for side in (-1, 1):
        x = 0.3
        k = 0
        while x < half - 0.2:
            x1 = min(x + seg, half - 0.2)
            a, b = sorted((side * x, side * x1))
            _paint(bm, _box(bm, a, b, -t, t, -t, t), "danger" if k % 2 == 0 else "stone")
            x, k = x1, k + 1
        a, b = sorted((side * (half - 0.2), side * half))
        _paint(bm, _box(bm, a, b, -t * 1.25, t * 1.25, -t * 1.25, t * 1.25), "danger_dark")
    return _object("Obstacle_KillBar_Bar", bm, col)


def build_jumppad(col):
    bm = bmesh.new()
    _paint(bm, _prism(bm, _chamfered_square(JUMPPAD_SIZE, 0.3), 0.0, JUMPPAD_HEIGHT), "stone_dark")
    _paint(bm, _prism(bm, _ngon(0.62, 8, math.pi / 8), JUMPPAD_HEIGHT, JUMPPAD_HEIGHT + 0.025), "rune")
    for x, y in _ngon(0.8, 4, math.pi / 4):
        _paint(bm, _prism(bm, [(x + px, y + py) for px, py in _ngon(0.07, 6)],
                          JUMPPAD_HEIGHT, JUMPPAD_HEIGHT + 0.04), "rune")
    return _object("Obstacle_JumpPad", bm, col)


def build_killsurface(name, size, col):
    bm = bmesh.new()
    _paint(bm, _prism(bm, _chamfered_square(size, 0.12), 0.0, KILL_BASE_HEIGHT), "danger_dark")
    n = int(round(size / SPIKE_SPACING))
    for i in range(n):
        for j in range(n):
            cx = -size / 2 + SPIKE_SPACING * (i + 0.5)
            cy = -size / 2 + SPIKE_SPACING * (j + 0.5)
            # Deterministic height variation so the bed looks hand-made.
            hv = 0.8 + 0.4 * ((i * 7 + j * 13) % 5) / 4
            _paint(bm, _pyramid(bm, cx, cy, SPIKE_BASE, KILL_BASE_HEIGHT, SPIKE_HEIGHT * hv),
                   "danger" if (i + j) % 2 == 0 else "danger_dark")
    return _object(name, bm, col)


def build_shard(col):
    bm = bmesh.new()
    _paint(bm, _bipyramid(bm, 0, 0, 0, 0.22, 0.47, 0.33), "crystal")
    _paint(bm, _bipyramid(bm, 0.18, 0.0, -0.11, 0.11, 0.24, 0.17, tilt=0.45), "crystal")
    return _object("Obstacle_Shard", bm, col)


BUILDERS = {
    "Obstacle_KillBar_Post": build_killbar_post,
    "Obstacle_KillBar_Bar": build_killbar_bar,
    "Obstacle_JumpPad": build_jumppad,
    "Obstacle_KillSurface_S": lambda col: build_killsurface("Obstacle_KillSurface_S", 2.0, col),
    "Obstacle_KillSurface_M": lambda col: build_killsurface("Obstacle_KillSurface_M", 4.0, col),
    "Obstacle_Shard": build_shard,
}


def get_collection():
    col = bpy.data.collections.get(COLLECTION)
    if col is None:
        col = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(col)
    return col


def build_all(spacing=6.5):
    col = get_collection()
    objs = []
    for i, builder in enumerate(BUILDERS.values()):
        obj = builder(col)
        obj.location.x = i * spacing
        objs.append(obj)
    return objs


def export_all(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    paths = []
    for name in BUILDERS:
        obj = bpy.data.objects.get(name)
        if obj is None:
            continue
        saved = obj.location.copy()
        obj.location = (0, 0, 0)
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        path = os.path.join(out_dir, f"{name}.fbx")
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
        )
        obj.location = saved
        paths.append(path)
    print("Exported", len(paths), "obstacle meshes to", out_dir)
    return paths


def _run_from_command_line():
    argv = sys.argv
    return ("--python" in argv and argv[argv.index("--python") + 1].endswith("obstacles.py")
            and "--" in argv)


if _run_from_command_line():
    build_all()
    export_all(sys.argv[sys.argv.index("--") + 1])
