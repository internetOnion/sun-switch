"""
Sun Switch: floating sky-ruin platform generator for Blender (5.x).

Builds stylised ruin platforms at exactly the grey-box sizes:
  - a paved top made of irregular stone tiles separated by shallow grooves,
    perfectly flat at z = 0 so it matches the Unity collider top
  - a chunky slab body with chipped corners (always inside the collider footprint)
  - a jagged rock hanging underneath, so the platforms read as floating sky-ruins

Blender is Z-up; the FBX export converts to Unity's Y-up. The pivot is the centre of
the top surface, same as the prefabs. Vertex colours tint each part (paving, grout,
sides, rock); the Unity Storybook shader multiplies them with the phase colour.

Run inside Blender (Scripting tab or the Blender MCP):
    exec(open("<repo>/ArtSource/ruin_platforms.py").read())
    build_all()                    # or build("Platform_Medium")
    export_all("<repo>/Assets/Art/Meshes/Ruins")
"""

import math
import random

import bmesh
import bpy
from mathutils import Vector

COLLECTION = "SunSwitch_Ruins"

TILE_SIZE = 1.0        # target paving stone size (m)
TILE_GAP = 0.03        # groove width between paving stones (m)
GROOVE_DEPTH = 0.02    # groove depth (m)
CHIP_MIN, CHIP_MAX = 0.06, 0.18   # corner chips cut into the footprint (m)
ROCK_SEGMENTS = 10
ROCK_MARGIN = 0.1      # rock stays this far inside the slab footprint (m)

# Vertex colour per part (multiplied with the phase material colour in Unity).
COLORS = {
    "tile": (1.0, 1.0, 1.0, 1.0),
    "grout": (0.72, 0.70, 0.76, 1.0),
    "side": (0.90, 0.88, 0.92, 1.0),
    "rock": (0.68, 0.66, 0.76, 1.0),
}
PARTS = list(COLORS)

# Same sizes as the grey-box prefabs (width x, depth y, thickness/height h), plus rock depth.
# Slabs and the pillar pivot at the top-surface centre; the wall pivots at the bottom centre.
SPECS = {
    "Platform_Small":  dict(kind="slab", w=2, d=2, h=0.5, rock=1.0, seed=11),
    "Platform_Medium": dict(kind="slab", w=4, d=4, h=0.5, rock=1.8, seed=23),
    "Platform_Wide":   dict(kind="slab", w=6, d=6, h=0.5, rock=2.4, seed=37),
    "Platform_Bridge": dict(kind="slab", w=2, d=8, h=0.4, rock=1.2, seed=41),
    "Platform_Moving": dict(kind="slab", w=3, d=3, h=0.4, rock=1.2, seed=53),
    "Checkpoint":      dict(kind="slab", w=4, d=4, h=0.5, rock=1.8, seed=67),
    "Platform_Pillar": dict(kind="pillar", w=2, d=2, h=1.0, rock=1.4, seed=71),
    "Wall":            dict(kind="wall", w=4, d=0.5, h=4.0, rock=0, seed=83),
}


# --------------------------------------------------------------------- 2D helpers

def chipped_rect(w, d, rng):
    """Rectangle footprint with each corner cut by a small random chip (CCW)."""
    hx, hy = w / 2, d / 2
    corners = [Vector((-hx, -hy)), Vector((hx, -hy)), Vector((hx, hy)), Vector((-hx, hy))]
    pts = []
    for i, c in enumerate(corners):
        incoming = (c - corners[i - 1]).normalized()
        outgoing = (corners[(i + 1) % 4] - c).normalized()
        pts.append(c - incoming * rng.uniform(CHIP_MIN, CHIP_MAX))
        pts.append(c + outgoing * rng.uniform(CHIP_MIN, CHIP_MAX))
    return pts


def offset_convex(poly, dist):
    """Move every edge of a CCW convex polygon inward by dist."""
    n = len(poly)
    lines = []
    for i in range(n):
        a, b = poly[i], poly[(i + 1) % n]
        e = (b - a).normalized()
        inward = Vector((-e.y, e.x))
        lines.append((a + inward * dist, e))
    out = []
    for i in range(n):
        p1, d1 = lines[i - 1]
        p2, d2 = lines[i]
        denom = d1.x * d2.y - d1.y * d2.x
        t = ((p2.x - p1.x) * d2.y - (p2.y - p1.y) * d2.x) / denom
        out.append(p1 + d1 * t)
    return out


def clip_convex(subject, clipper):
    """Sutherland-Hodgman: clip polygon 'subject' by CCW convex polygon 'clipper'."""
    def inside(p, a, b):
        return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x) >= -1e-9

    def intersect(p, q, a, b):
        r, s = q - p, b - a
        denom = r.x * s.y - r.y * s.x
        t = ((a.x - p.x) * s.y - (a.y - p.y) * s.x) / denom
        return p + r * t

    out = subject
    for i in range(len(clipper)):
        a, b = clipper[i], clipper[(i + 1) % len(clipper)]
        inp, out = out, []
        if not inp:
            break
        for j in range(len(inp)):
            p, q = inp[j], inp[(j + 1) % len(inp)]
            if inside(q, a, b):
                if not inside(p, a, b):
                    out.append(intersect(p, q, a, b))
                out.append(q)
            elif inside(p, a, b):
                out.append(intersect(p, q, a, b))
    return out


def paving_rects(w, d, rng):
    """Running-bond paving: rows of uneven depth, each split at random widths."""
    rows = max(1, round(d / TILE_SIZE))
    edges_y = [-d / 2 + d * i / rows for i in range(rows + 1)]
    for i in range(1, rows):
        edges_y[i] += rng.uniform(-0.15, 0.15) * d / rows
    rects = []
    for r in range(rows):
        y0, y1 = edges_y[r], edges_y[r + 1]
        cols = max(1, round(w / TILE_SIZE))
        xs = [-w / 2]
        x = -w / 2 + (0.5 if r % 2 else 1.0) * w / cols * rng.uniform(0.8, 1.2)
        while x < w / 2 - 0.35:
            xs.append(x)
            x += w / cols * rng.uniform(0.75, 1.25)
        xs.append(w / 2)
        for c in range(len(xs) - 1):
            rects.append((xs[c], y0, xs[c + 1], y1))
    return rects


# --------------------------------------------------------------------- mesh building

class Builder:
    def __init__(self):
        self.bm = bmesh.new()
        self.part = self.bm.faces.layers.int.new("part")

    def face(self, verts, part):
        f = self.bm.faces.new(verts)
        f[self.part] = PARTS.index(part)
        return f

    def prism(self, poly, z_top, z_bottom, top_part, side_part, cap_bottom=False):
        top = [self.bm.verts.new((p.x, p.y, z_top)) for p in poly]
        bot = [self.bm.verts.new((p.x, p.y, z_bottom)) for p in poly]
        self.face(top, top_part)
        n = len(poly)
        for i in range(n):
            j = (i + 1) % n
            self.face([bot[i], bot[j], top[j], top[i]], side_part)
        if cap_bottom:
            self.face(list(reversed(bot)), side_part)
        return bot

    def rock(self, outline, z_top, depth, rng):
        """Jagged cone hanging below the slab, tucked slightly inside the body."""
        centre = sum(outline, Vector((0, 0))) / len(outline)
        radius_x = max(abs(p.x - centre.x) for p in outline)
        radius_y = max(abs(p.y - centre.y) for p in outline)
        min_x, max_x = min(p.x for p in outline), max(p.x for p in outline)
        min_y, max_y = min(p.y for p in outline), max(p.y for p in outline)
        self.rock_bounds = (min_x + ROCK_MARGIN, max_x - ROCK_MARGIN, min_y + ROCK_MARGIN, max_y - ROCK_MARGIN)
        # (depth fraction, radius scale): bulges out just under the slab, then tapers unevenly.
        rings = [(0.0, 0.88), (0.18, 0.92), (0.45, 0.62), (0.72, 0.36)]
        drift = Vector((rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25)))  # rock leans to one side
        ring_verts = []
        for t, scale in rings:
            ring = []
            twist = rng.uniform(-0.3, 0.3)
            for s in range(ROCK_SEGMENTS):
                a = s / ROCK_SEGMENTS * math.tau + twist + rng.uniform(-0.15, 0.15)
                jitter = 1.0 if t == 0 else rng.uniform(0.7, 1.25)
                offset = drift * t * min(radius_x, radius_y)
                x = centre.x + offset.x + math.cos(a) * radius_x * scale * jitter
                y = centre.y + offset.y + math.sin(a) * radius_y * scale * jitter
                # Never wider than the slab: from above it must not look like standable ground.
                x = min(max(x, min_x + ROCK_MARGIN), max_x - ROCK_MARGIN)
                y = min(max(y, min_y + ROCK_MARGIN), max_y - ROCK_MARGIN)
                z = z_top + 0.05 - t * depth + (0 if t == 0 else rng.uniform(-0.1, 0.1) * depth)
                ring.append(self.bm.verts.new((x, y, z)))
            ring_verts.append(ring)
        tip_offset = drift * min(radius_x, radius_y)
        tip = self.bm.verts.new((centre.x + tip_offset.x, centre.y + tip_offset.y, z_top - depth))
        for r in range(len(ring_verts) - 1):
            upper, lower = ring_verts[r], ring_verts[r + 1]
            for s in range(ROCK_SEGMENTS):
                n = (s + 1) % ROCK_SEGMENTS
                self.face([lower[s], lower[n], upper[n], upper[s]], "rock")
        last = ring_verts[-1]
        for s in range(ROCK_SEGMENTS):
            self.face([last[(s + 1) % ROCK_SEGMENTS], last[s], tip], "rock")

        # A couple of small stalactite spurs hanging off the upper rock.
        for _ in range(rng.randint(1, 3)):
            ring_idx = rng.choice((1, 2))
            s = rng.randrange(ROCK_SEGMENTS)
            base = ring_verts[ring_idx][s].co
            toward = Vector((base.x - centre.x, base.y - centre.y)).normalized() * 0.15
            self.spur(Vector((base.x - toward.x, base.y - toward.y, base.z + 0.05)),
                      rng.uniform(0.15, 0.3) * min(radius_x, radius_y), rng.uniform(0.3, 0.55) * depth, rng)

    def spur(self, top_centre, radius, length, rng):
        n = 5
        ring = []
        for s in range(n):
            a = s / n * math.tau + rng.uniform(-0.2, 0.2)
            r = radius * rng.uniform(0.8, 1.2)
            x0, x1, y0, y1 = self.rock_bounds
            x = min(max(top_centre.x + math.cos(a) * r, x0), x1)
            y = min(max(top_centre.y + math.sin(a) * r, y0), y1)
            ring.append(self.bm.verts.new((x, y, top_centre.z)))
        tip = self.bm.verts.new((top_centre.x + rng.uniform(-0.1, 0.1), top_centre.y + rng.uniform(-0.1, 0.1), top_centre.z - length))
        for s in range(n):
            self.face([ring[(s + 1) % n], ring[s], tip], "rock")

    def to_object(self, name, collection):
        mesh = bpy.data.meshes.new(name)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for poly in mesh.polygons:
            poly.use_smooth = False

        parts = mesh.attributes["part"].data
        col = mesh.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
        for poly in mesh.polygons:
            c = COLORS[PARTS[parts[poly.index].value]]
            for li in poly.loop_indices:
                col.data[li].color = c
        mesh.color_attributes.active_color = col
        mesh.attributes.remove(mesh.attributes["part"])

        obj = bpy.data.objects.new(name, mesh)
        collection.objects.link(obj)
        return obj


def build_slab(name, w, d, h, rock, seed, collection):
    rng = random.Random(seed)
    b = Builder()
    outline = chipped_rect(w, d, rng)

    # Body: grout level on top, then two stone courses; the lower one steps in slightly
    # so the side reads as stacked masonry (and gets an inked ledge line in Unity).
    course = -h * 0.55
    b.prism(outline, -GROOVE_DEPTH, course, "grout", "side", cap_bottom=True)
    lower = offset_convex(outline, 0.05)
    b.prism(lower, course, -h, "side", "side", cap_bottom=True)

    # Paving stones: flat tops at z = 0, clipped to the chipped footprint.
    tile_bounds = offset_convex(outline, TILE_GAP / 2)
    for x0, y0, x1, y1 in paving_rects(w, d, rng):
        g = TILE_GAP / 2
        rect = [Vector((x0 + g, y0 + g)), Vector((x1 - g, y0 + g)), Vector((x1 - g, y1 - g)), Vector((x0 + g, y1 - g))]
        tile = clip_convex(rect, tile_bounds)
        if len(tile) >= 3:
            b.prism(tile, 0.0, -GROOVE_DEPTH, "tile", "tile")

    b.rock(outline, -h, rock, rng)
    return b.to_object(name, collection)


def ngon(radius, sides, rng, jitter=0.0, phase=0.0):
    return [Vector((math.cos(a) * radius * (1 - rng.uniform(0, jitter)),
                    math.sin(a) * radius * (1 - rng.uniform(0, jitter))))
            for a in (phase + i / sides * math.tau for i in range(sides))]


def shrink(poly, amount):
    """Pull a convex polygon toward its centroid by 'amount' metres (for groove gaps)."""
    c = sum(poly, Vector((0, 0))) / len(poly)
    return [c + (p - c) * max(0.0, 1 - amount / max((p - c).length, 1e-6)) for p in poly]


def build_pillar(name, w, d, h, rock, seed, collection):
    """Broken column: a round capital of ring stones, a faceted shaft, a rock underneath."""
    rng = random.Random(seed)
    b = Builder()
    sides = 10
    radius = w / 2
    capital_depth = 0.22
    outer = ngon(radius, sides, rng)               # never wider than the collider
    inner = ngon(radius * 0.55, sides, rng, phase=0.0)

    # Capital body below grout level.
    b.prism(outer, -GROOVE_DEPTH, -capital_depth, "grout", "side", cap_bottom=True)
    # Ring of wedge-shaped stones plus a centre disc, all flat at z = 0.
    for i in range(sides):
        j = (i + 1) % sides
        stone = shrink([inner[i], outer[i], outer[j], inner[j]], TILE_GAP / 2)
        b.prism(stone, 0.0, -GROOVE_DEPTH, "tile", "tile")
    b.prism(shrink(inner, TILE_GAP / 2), 0.0, -GROOVE_DEPTH, "tile", "tile")

    # Faceted shaft (each facet gets an ink line, like flutes) down to the collider bottom.
    shaft = ngon(radius * 0.8, sides, rng, jitter=0.03, phase=math.pi / sides)
    b.prism(shaft, -capital_depth, -h, "side", "side", cap_bottom=True)
    b.rock(shaft, -h, rock, rng)
    return b.to_object(name, collection)


def build_wall(name, w, d, h, rock, seed, collection):
    """Crumbling brick wall, pivot at the bottom centre, a few bricks missing from the top."""
    rng = random.Random(seed)
    b = Builder()
    t = d / 2
    course = 0.5
    rows = max(1, round(h / course))
    g = TILE_GAP / 2

    def box(x0, x1, y0, y1, z0, z1, part):
        v = [b.bm.verts.new(p) for p in (
            (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
            (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1))]
        for idx in ((0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)):
            b.face([v[i] for i in idx], part)

    # Mortar core, one course shorter so broken top bricks reveal it.
    box(-w / 2 + g, w / 2 - g, -t + GROOVE_DEPTH, t - GROOVE_DEPTH, 0.0, h - course, "grout")

    for r in range(rows):
        z0, z1 = r * course, (r + 1) * course
        top_row = r == rows - 1
        xs = [-w / 2]
        x = -w / 2 + course * (1.0 if r % 2 else 2.0) * rng.uniform(0.8, 1.2)
        while x < w / 2 - 0.3:
            xs.append(x)
            x += rng.uniform(0.7, 1.2)
        xs.append(w / 2)
        for c in range(len(xs) - 1):
            top = z1
            if top_row:
                if rng.random() < 0.3:
                    continue                      # missing brick
                top = z1 - rng.uniform(0.0, 0.2)  # weathered, uneven top
            box(xs[c] + g, xs[c + 1] - g, -t, t, z0 + g, top - g, "side")
    return b.to_object(name, collection)


BUILDERS = {"slab": build_slab, "pillar": build_pillar, "wall": build_wall}


# --------------------------------------------------------------------- scene API

def get_collection():
    col = bpy.data.collections.get(COLLECTION)
    if col is None:
        col = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(col)
    return col


def build(name, x_offset=0.0):
    col = get_collection()
    old = bpy.data.objects.get(name)
    if old is not None:
        mesh = old.data
        bpy.data.objects.remove(old)
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    spec = SPECS[name]
    obj = BUILDERS[spec["kind"]](name, spec["w"], spec["d"], spec["h"], spec["rock"], spec["seed"], col)
    obj.location.x = x_offset
    return obj


def build_all(spacing=2.0):
    x = 0.0
    objs = []
    for name, spec in SPECS.items():
        x += spec["w"] / 2
        objs.append(build(name, x))
        x += spec["w"] / 2 + spacing
    return objs


def export_all(out_dir):
    import os
    os.makedirs(out_dir, exist_ok=True)
    paths = []
    for name in SPECS:
        obj = bpy.data.objects.get(name)
        if obj is None:
            continue
        saved_loc = obj.location.copy()
        obj.location = (0, 0, 0)
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        path = os.path.join(out_dir, f"Ruin_{name}.fbx")
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
            colors_type="SRGB",
            add_leaf_bones=False,
            use_mesh_modifiers=True,
        )
        obj.location = saved_loc
        paths.append(path)
    return paths
