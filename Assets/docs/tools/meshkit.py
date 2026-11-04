"""Mini toolkit per costruire mesh low-poly e scriverle in GLB (glTF 2.0 binario).

Convenzioni glTF: metri, Y in alto, fronte del modello verso +Z, triangoli CCW.
"""
import json
import math
import struct

import numpy as np


class Geom:
    """Mesh indicizzata: posizioni, normali, UV e triangoli."""

    def __init__(self, p=None, n=None, uv=None, idx=None):
        self.p = np.zeros((0, 3), np.float32) if p is None else np.asarray(p, np.float32)
        self.n = np.zeros((0, 3), np.float32) if n is None else np.asarray(n, np.float32)
        self.uv = np.zeros((0, 2), np.float32) if uv is None else np.asarray(uv, np.float32)
        self.idx = np.zeros((0, 3), np.uint32) if idx is None else np.asarray(idx, np.uint32)

    def __add__(self, other):
        off = len(self.p)
        return Geom(np.vstack([self.p, other.p]), np.vstack([self.n, other.n]),
                    np.vstack([self.uv, other.uv]), np.vstack([self.idx, other.idx + off]))

    def transformed(self, rot=None, t=(0, 0, 0)):
        rot = np.eye(3) if rot is None else np.asarray(rot)
        t = (0, 0, 0) if t is None else t
        p = self.p @ rot.T + np.asarray(t, np.float32)
        n = self.n @ rot.T
        g = Geom(p, n, self.uv.copy(), self.idx.copy())
        g.fix_winding()
        return g

    def fix_winding(self):
        """Gira i triangoli la cui normale geometrica è opposta alle normali dei vertici."""
        if len(self.idx) == 0:
            return self
        a, b, c = (self.p[self.idx[:, k]] for k in range(3))
        gn = np.cross(b - a, c - a)
        vn = self.n[self.idx[:, 0]] + self.n[self.idx[:, 1]] + self.n[self.idx[:, 2]]
        bad = np.einsum("ij,ij->i", gn, vn) < 0
        self.idx[bad] = self.idx[bad][:, [0, 2, 1]]
        return self


def merge(geoms):
    out = Geom()
    for g in geoms:
        out = out + g
    return out


def rot_x(deg):
    a = math.radians(deg)
    return np.array([[1, 0, 0], [0, math.cos(a), -math.sin(a)], [0, math.sin(a), math.cos(a)]])


def rot_y(deg):
    a = math.radians(deg)
    return np.array([[math.cos(a), 0, math.sin(a)], [0, 1, 0], [-math.sin(a), 0, math.cos(a)]])


def rot_z(deg):
    a = math.radians(deg)
    return np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1]])


def quat_from_axis(axis, deg):
    a = math.radians(deg) / 2
    ax = np.asarray(axis, float)
    ax = ax / np.linalg.norm(ax)
    return [float(ax[0] * math.sin(a)), float(ax[1] * math.sin(a)), float(ax[2] * math.sin(a)), float(math.cos(a))]


# Rotazioni che portano l'asse locale Z sull'asse richiesto
AXIS_ROT = {"z": np.eye(3), "x": rot_y(90), "y": rot_x(-90)}


def quad(corners, normal, uvs):
    g = Geom(corners, [normal] * 4, uvs, [[0, 1, 2], [0, 2, 3]])
    return g.fix_winding()


def box(center, size, uv="world", uv_scale=1.0):
    """Parallelepipedo con normali piatte.

    uv='world': UV in metri (per materiali ripetibili). uv='unit': ogni faccia 0..1.
    Sulla faccia +Z: u verso +X, v verso il basso (convenzione glTF, origine in alto a sinistra).
    """
    cx, cy, cz = center
    hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
    faces = []
    # (normale, asse u, asse v) con u e v come vettori unitari nello spazio
    specs = [
        ((0, 0, 1), (1, 0, 0), (0, -1, 0)),
        ((0, 0, -1), (-1, 0, 0), (0, -1, 0)),
        ((1, 0, 0), (0, 0, -1), (0, -1, 0)),
        ((-1, 0, 0), (0, 0, 1), (0, -1, 0)),
        ((0, 1, 0), (1, 0, 0), (0, 0, 1)),
        ((0, -1, 0), (1, 0, 0), (0, 0, -1)),
    ]
    half = np.array([hx, hy, hz])
    c = np.array(center, float)
    for n, u, v in specs:
        n, u, v = np.array(n, float), np.array(u, float), np.array(v, float)
        fc = c + n * half
        eu = u * half
        ev = v * half
        corners = [fc - eu - ev, fc + eu - ev, fc + eu + ev, fc - eu + ev]
        if uv == "unit":
            uvs = [(0, 0), (1, 0), (1, 1), (0, 1)]
        else:
            lu, lv = 2 * np.abs(eu).sum(), 2 * np.abs(ev).sum()
            ou = float(np.dot(fc - eu, u))
            ov = float(np.dot(fc - ev, v))
            uvs = [(ou * uv_scale, ov * uv_scale), ((ou + lu) * uv_scale, ov * uv_scale),
                   ((ou + lu) * uv_scale, (ov + lv) * uv_scale), (ou * uv_scale, (ov + lv) * uv_scale)]
        faces.append(quad(corners, n, uvs))
    return merge(faces)


def decal(center, size, uv_repeat=(1, 1)):
    """Quad rivolto verso +Z con UV 0..1 (o ripetute), per etichette e cartelli."""
    cx, cy, cz = center
    w, h = size
    corners = [(cx - w / 2, cy + h / 2, cz), (cx + w / 2, cy + h / 2, cz),
               (cx + w / 2, cy - h / 2, cz), (cx - w / 2, cy - h / 2, cz)]
    ru, rv = uv_repeat
    return quad(corners, (0, 0, 1), [(0, 0), (ru, 0), (ru, rv), (0, rv)])


def cylinder(center, radius, length, axis="z", seg=32, caps=True):
    """Cilindro: restituisce dict con 'side', 'front' (+asse) e 'back' (-asse).

    I tappi hanno UV 0..1 inscritte nel disco: su un tappo +Z, u verso +X e v verso -Y.
    """
    a = np.linspace(0, 2 * math.pi, seg + 1)
    z0, z1 = -length / 2, length / 2
    p, n, uv, idx = [], [], [], []
    circ = 2 * math.pi * radius
    for i, t in enumerate(a):
        x, y = math.cos(t), math.sin(t)
        p += [(radius * x, radius * y, z0), (radius * x, radius * y, z1)]
        n += [(x, y, 0), (x, y, 0)]
        uv += [(t / (2 * math.pi) * circ, length), (t / (2 * math.pi) * circ, 0)]
    for i in range(seg):
        a0, a1, b0, b1 = 2 * i, 2 * i + 1, 2 * i + 2, 2 * i + 3
        idx += [(a0, b0, b1), (a0, b1, a1)]
    side = Geom(p, n, uv, idx).fix_winding()
    parts = {"side": side}
    if caps:
        for name, z, nz in (("front", z1, 1), ("back", z0, -1)):
            cp = [(0, 0, z)] + [(radius * math.cos(t), radius * math.sin(t), z) for t in a[:-1]]
            cuv = [(0.5, 0.5)] + [(0.5 + 0.5 * math.cos(t) * nz, 0.5 - 0.5 * math.sin(t)) for t in a[:-1]]
            cidx = [(0, 1 + i, 1 + (i + 1) % seg) for i in range(seg)]
            parts[name] = Geom(cp, [(0, 0, nz)] * len(cp), cuv, cidx).fix_winding()
    rot = AXIS_ROT[axis]
    return {k: v.transformed(rot, center) for k, v in parts.items()}


def cyl(center, radius, length, axis="z", seg=32):
    """Cilindro chiuso come unica Geom."""
    return merge(cylinder(center, radius, length, axis, seg).values())


def tube(center, r_out, r_in, length, axis="z", seg=32):
    """Cilindro cavo (anello con spessore)."""
    a = np.linspace(0, 2 * math.pi, seg + 1)
    z0, z1 = -length / 2, length / 2
    geoms = []
    for r, sgn in ((r_out, 1), (r_in, -1)):
        p, n, uv, idx = [], [], [], []
        for t in a:
            x, y = math.cos(t), math.sin(t)
            p += [(r * x, r * y, z0), (r * x, r * y, z1)]
            n += [(sgn * x, sgn * y, 0)] * 2
            uv += [(t * r, length), (t * r, 0)]
        for i in range(seg):
            idx += [(2 * i, 2 * i + 2, 2 * i + 3), (2 * i, 2 * i + 3, 2 * i + 1)]
        geoms.append(Geom(p, n, uv, idx).fix_winding())
    for z, nz in ((z1, 1), (z0, -1)):
        p, n, uv, idx = [], [], [], []
        for t in a:
            x, y = math.cos(t), math.sin(t)
            p += [(r_out * x, r_out * y, z), (r_in * x, r_in * y, z)]
            n += [(0, 0, nz)] * 2
            uv += [(r_out * x, -r_out * y), (r_in * x, -r_in * y)]
        for i in range(seg):
            idx += [(2 * i, 2 * i + 2, 2 * i + 3), (2 * i, 2 * i + 3, 2 * i + 1)]
        geoms.append(Geom(p, n, uv, idx).fix_winding())
    return merge(geoms).transformed(AXIS_ROT[axis], center)


def dome(center, radius, height, seg=32, rings=8):
    """Calotta sferica rivolta verso +Z, con base sul piano z=center.z."""
    R = (radius ** 2 + height ** 2) / (2 * height)
    zc = height - R
    phi_max = math.asin(radius / R)
    p, n, uv, idx = [], [], [], []
    for j in range(rings + 1):
        phi = phi_max * j / rings
        for i in range(seg + 1):
            t = 2 * math.pi * i / seg
            nx, ny, nz = math.sin(phi) * math.cos(t), math.sin(phi) * math.sin(t), math.cos(phi)
            p.append((R * nx, R * ny, zc + R * nz))
            n.append((nx, ny, nz))
            uv.append((0.5 + 0.5 * nx * R / radius, 0.5 - 0.5 * ny * R / radius))
    for j in range(rings):
        for i in range(seg):
            a = j * (seg + 1) + i
            b = a + seg + 1
            idx += [(a, b, b + 1), (a, b + 1, a + 1)]
    return Geom(p, n, uv, idx).fix_winding().transformed(None, center)


def torus_arc(center, R, r, a0, a1, seg=24, seg_minor=12):
    """Arco di toro nel piano XY (per l'arco del lucchetto). Angoli in gradi."""
    p, n, uv, idx = [], [], [], []
    for i in range(seg + 1):
        t = math.radians(a0 + (a1 - a0) * i / seg)
        cx, cy = R * math.cos(t), R * math.sin(t)
        for j in range(seg_minor + 1):
            s = 2 * math.pi * j / seg_minor
            nx = math.cos(s) * math.cos(t)
            ny = math.cos(s) * math.sin(t)
            nz = math.sin(s)
            p.append((cx + r * nx, cy + r * ny, r * nz))
            n.append((nx, ny, nz))
            uv.append((i / seg, j / seg_minor))
    for i in range(seg):
        for j in range(seg_minor):
            a = i * (seg_minor + 1) + j
            b = a + seg_minor + 1
            idx += [(a, b, b + 1), (a, b + 1, a + 1)]
    return Geom(p, n, uv, idx).fix_winding().transformed(None, center)


def sphere(center, radius, seg=20, rings=12):
    p, n, uv, idx = [], [], [], []
    for j in range(rings + 1):
        phi = math.pi * j / rings
        for i in range(seg + 1):
            t = 2 * math.pi * i / seg
            nx, ny, nz = math.sin(phi) * math.cos(t), math.cos(phi), math.sin(phi) * math.sin(t)
            p.append((radius * nx, radius * ny, radius * nz))
            n.append((nx, ny, nz))
            uv.append((i / seg, j / rings))
    for j in range(rings):
        for i in range(seg):
            a = j * (seg + 1) + i
            b = a + seg + 1
            idx += [(a, b, b + 1), (a, b + 1, a + 1)]
    return Geom(p, n, uv, idx).fix_winding().transformed(None, center)


class Node:
    """Nodo della gerarchia: nome, trasformazione locale, parti (geom, materiale), figli."""

    def __init__(self, name, t=(0, 0, 0), r=None, parts=None, children=None, extras=None):
        self.name = name
        self.t = [float(x) for x in t]
        self.r = r
        self.parts = parts or []
        self.children = children or []
        self.extras = extras

    def add(self, *nodes):
        self.children.extend(nodes)
        return self


class GlbWriter:
    def __init__(self, materials, texture_dir):
        self.mat_defs = materials
        self.texture_dir = texture_dir
        self.bin = bytearray()
        self.gltf = {"asset": {"version": "2.0", "generator": "VRPT meshkit"},
                     "scene": 0, "scenes": [{"nodes": []}], "nodes": [], "meshes": [],
                     "materials": [], "accessors": [], "bufferViews": [], "buffers": []}
        self.mat_index = {}
        self.tex_index = {}

    def _view(self, data, target=None):
        while len(self.bin) % 4:
            self.bin.append(0)
        off = len(self.bin)
        self.bin += data
        view = {"buffer": 0, "byteOffset": off, "byteLength": len(data)}
        if target:
            view["target"] = target
        self.gltf["bufferViews"].append(view)
        return len(self.gltf["bufferViews"]) - 1

    def _accessor(self, arr, kind, comp, target, minmax=False):
        view = self._view(arr.tobytes(), target)
        acc = {"bufferView": view, "componentType": comp, "count": int(arr.shape[0]), "type": kind}
        if minmax:
            acc["min"] = [float(x) for x in arr.min(axis=0)]
            acc["max"] = [float(x) for x in arr.max(axis=0)]
        self.gltf["accessors"].append(acc)
        return len(self.gltf["accessors"]) - 1

    def _texture(self, fname):
        if fname in self.tex_index:
            return self.tex_index[fname]
        with open(f"{self.texture_dir}/{fname}", "rb") as f:
            data = f.read()
        g = self.gltf
        g.setdefault("images", []).append({"name": fname.rsplit(".", 1)[0], "mimeType": "image/png",
                                           "bufferView": self._view(data)})
        g.setdefault("samplers", [{"magFilter": 9729, "minFilter": 9987, "wrapS": 10497, "wrapT": 10497}])
        g.setdefault("textures", []).append({"source": len(g["images"]) - 1, "sampler": 0})
        self.tex_index[fname] = len(g["textures"]) - 1
        return self.tex_index[fname]

    def _material(self, name):
        if name in self.mat_index:
            return self.mat_index[name]
        d = self.mat_defs[name]
        pbr = {"baseColorFactor": list(d.get("color", (1, 1, 1))) + [d.get("alpha", 1.0)],
               "metallicFactor": d.get("metallic", 0.0), "roughnessFactor": d.get("roughness", 0.6)}
        if "texture" in d:
            pbr["baseColorTexture"] = {"index": self._texture(d["texture"])}
        m = {"name": name, "pbrMetallicRoughness": pbr}
        if d.get("alpha", 1.0) < 1.0 or d.get("blend"):
            m["alphaMode"] = "BLEND"
        if d.get("mask"):
            m["alphaMode"] = "MASK"
            m["alphaCutoff"] = 0.5
        if d.get("double"):
            m["doubleSided"] = True
        if "emissive" in d:
            m["emissiveFactor"] = list(d["emissive"])
        self.gltf["materials"].append(m)
        self.mat_index[name] = len(self.gltf["materials"]) - 1
        return self.mat_index[name]

    def _mesh(self, name, parts):
        by_mat = {}
        for geom, mat in parts:
            by_mat.setdefault(mat, []).append(geom)
        prims = []
        for mat, geoms in by_mat.items():
            g = merge(geoms)
            nrm = g.n / np.maximum(np.linalg.norm(g.n, axis=1, keepdims=True), 1e-8)
            attrs = {
                "POSITION": self._accessor(g.p.astype(np.float32), "VEC3", 5126, 34962, True),
                "NORMAL": self._accessor(nrm.astype(np.float32), "VEC3", 5126, 34962),
                "TEXCOORD_0": self._accessor(g.uv.astype(np.float32), "VEC2", 5126, 34962),
            }
            ind = self._accessor(g.idx.astype(np.uint32).reshape(-1), "SCALAR", 5125, 34963)
            prims.append({"attributes": attrs, "indices": ind, "material": self._material(mat)})
        self.gltf["meshes"].append({"name": name, "primitives": prims})
        return len(self.gltf["meshes"]) - 1

    def _node(self, node):
        entry = {"name": node.name}
        if any(abs(x) > 1e-9 for x in node.t):
            entry["translation"] = node.t
        if node.r is not None:
            entry["rotation"] = node.r
        if node.extras:
            entry["extras"] = node.extras
        if node.parts:
            entry["mesh"] = self._mesh(node.name, node.parts)
        idx = len(self.gltf["nodes"])
        self.gltf["nodes"].append(entry)
        kids = [self._node(c) for c in node.children]
        if kids:
            self.gltf["nodes"][idx]["children"] = kids
        return idx

    def write(self, root, path):
        self.gltf["scenes"][0]["nodes"] = [self._node(root)]
        while len(self.bin) % 4:
            self.bin.append(0)
        self.gltf["buffers"] = [{"byteLength": len(self.bin)}]
        js = json.dumps(self.gltf, separators=(",", ":")).encode()
        while len(js) % 4:
            js += b" "
        total = 12 + 8 + len(js) + 8 + len(self.bin)
        with open(path, "wb") as f:
            f.write(struct.pack("<III", 0x46546C67, 2, total))
            f.write(struct.pack("<II", len(js), 0x4E4F534A))
            f.write(js)
            f.write(struct.pack("<II", len(self.bin), 0x004E4942))
            f.write(self.bin)
