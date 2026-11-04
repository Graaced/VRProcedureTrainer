"""Rilegge i GLB generati, ne valida la struttura e li renderizza con un piccolo rasterizzatore.

Il render serve a controllare forma, pivot, orientamento delle texture e pose di prova
(sezionatore in OFF con lucchetto, carter aperto, lancetta del manometro).
"""
import io
import json
import math
import struct
import sys

import numpy as np
from PIL import Image

COMP = {5126: (np.float32, 4), 5125: (np.uint32, 4), 5123: (np.uint16, 2)}
NCOMP = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}


def load_glb(path):
    data = open(path, "rb").read()
    magic, ver, total = struct.unpack_from("<III", data, 0)
    assert magic == 0x46546C67 and ver == 2 and total == len(data), "header GLB non valido"
    jlen, jtype = struct.unpack_from("<II", data, 12)
    assert jtype == 0x4E4F534A and jlen % 4 == 0
    g = json.loads(data[20:20 + jlen])
    blen, btype = struct.unpack_from("<II", data, 20 + jlen)
    assert btype == 0x004E4942 and blen % 4 == 0
    binary = data[28 + jlen:28 + jlen + blen]
    assert g["buffers"][0]["byteLength"] == blen
    return g, binary


def accessor(g, binary, i):
    a = g["accessors"][i]
    v = g["bufferViews"][a["bufferView"]]
    dt, size = COMP[a["componentType"]]
    n = NCOMP[a["type"]]
    start = v.get("byteOffset", 0) + a.get("byteOffset", 0)
    assert start % size == 0, "accessor non allineato"
    end = start + a["count"] * n * size
    assert end <= v.get("byteOffset", 0) + v["byteLength"] <= len(binary), "accessor fuori dal buffer"
    arr = np.frombuffer(binary, dt, a["count"] * n, start)
    return arr.reshape(a["count"], n) if n > 1 else arr


def quat_mat(q):
    x, y, z, w = q
    return np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                     [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                     [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])


def node_matrix(n):
    m = np.eye(4)
    if "rotation" in n:
        m[:3, :3] = quat_mat(n["rotation"])
    if "translation" in n:
        m[:3, 3] = n["translation"]
    return m


def validate(g, binary):
    """Controlli strutturali; restituisce statistiche."""
    tris = 0
    images = {}
    for i, img in enumerate(g.get("images", [])):
        v = g["bufferViews"][img["bufferView"]]
        raw = binary[v["byteOffset"]:v["byteOffset"] + v["byteLength"]]
        images[i] = Image.open(io.BytesIO(raw)).convert("RGBA")
    for mesh in g["meshes"]:
        for p in mesh["primitives"]:
            pos = accessor(g, binary, p["attributes"]["POSITION"])
            nrm = accessor(g, binary, p["attributes"]["NORMAL"])
            uv = accessor(g, binary, p["attributes"]["TEXCOORD_0"])
            idx = accessor(g, binary, p["indices"])
            assert len(pos) == len(nrm) == len(uv)
            assert idx.max() < len(pos), "indice fuori range"
            assert len(idx) % 3 == 0
            assert np.allclose(np.linalg.norm(nrm, axis=1), 1, atol=1e-3), "normali non unitarie"
            a = g["accessors"][p["attributes"]["POSITION"]]
            assert np.allclose(a["min"], pos.min(0), atol=1e-5) and np.allclose(a["max"], pos.max(0), atol=1e-5)
            assert 0 <= p["material"] < len(g["materials"])
            tris += len(idx) // 3
    names = [n["name"] for n in g["nodes"]]
    return {"triangoli": tris, "nodi": len(names), "materiali": len(g["materials"]),
            "texture": len(images)}, images


def collect(g, binary, images, overrides=None):
    """Triangoli in coordinate mondo. overrides: nome nodo -> matrice locale sostitutiva."""
    overrides = overrides or {}
    out = []

    def walk(i, parent):
        n = g["nodes"][i]
        local = overrides.get(n["name"], node_matrix(n))
        world = parent @ local
        if "mesh" in n:
            for p in g["meshes"][n["mesh"]]["primitives"]:
                pos = accessor(g, binary, p["attributes"]["POSITION"])
                nrm = accessor(g, binary, p["attributes"]["NORMAL"])
                uv = accessor(g, binary, p["attributes"]["TEXCOORD_0"])
                idx = accessor(g, binary, p["indices"]).reshape(-1, 3)
                wp = pos @ world[:3, :3].T + world[:3, 3]
                wn = nrm @ world[:3, :3].T
                mat = g["materials"][p["material"]]
                pbr = mat["pbrMetallicRoughness"]
                tex = None
                if "baseColorTexture" in pbr:
                    tex = images[g["textures"][pbr["baseColorTexture"]["index"]]["source"]]
                out.append((wp, wn, uv, idx, pbr["baseColorFactor"], pbr.get("metallicFactor", 0),
                            tex, mat.get("alphaMode", "OPAQUE")))
        for c in n.get("children", []):
            walk(c, world)

    for r in g["scenes"][0]["nodes"]:
        walk(r, np.eye(4))
    return out


def lin2srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def render(prims, eye, target, size=(900, 700), fov=35, light=(0.4, 0.8, 0.6)):
    W, H = size
    eye, target = np.array(eye, float), np.array(target, float)
    fwd = target - eye
    fwd /= np.linalg.norm(fwd)
    right = np.cross(fwd, [0, 1, 0])
    right /= np.linalg.norm(right)
    up = np.cross(right, fwd)
    f = 1 / math.tan(math.radians(fov) / 2)
    L = np.array(light, float)
    L /= np.linalg.norm(L)

    img = np.zeros((H, W, 3))
    grad = np.linspace(0.30, 0.18, H)[:, None]
    img[:] = grad[..., None] * np.array([1.0, 1.0, 1.05])
    zbuf = np.full((H, W), np.inf)

    blended = []
    for prim in prims:
        if prim[7] == "BLEND":
            blended.append(prim)
            continue
        raster(prim, eye, fwd, right, up, f, W, H, L, img, zbuf, False)
    for prim in blended:
        raster(prim, eye, fwd, right, up, f, W, H, L, img, zbuf, True)
    return Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8))


def raster(prim, eye, fwd, right, up, f, W, H, L, img, zbuf, blend):
    wp, wn, uv, idx, color, metallic, tex, mode = prim
    rel = wp - eye
    vx, vy, vz = rel @ right, rel @ up, rel @ fwd
    aspect = W / H
    sx = (vx / np.maximum(vz, 1e-4) * f / aspect * 0.5 + 0.5) * W
    sy = (0.5 - vy / np.maximum(vz, 1e-4) * f * 0.5) * H
    base = np.array(color[:3])
    alpha = color[3]
    texarr = np.asarray(tex, float) / 255.0 if tex is not None else None
    for t in idx:
        if (vz[t] < 0.01).any():
            continue
        x, y, z = sx[t], sy[t], vz[t]
        area = (x[1] - x[0]) * (y[2] - y[0]) - (x[2] - x[0]) * (y[1] - y[0])
        if abs(area) < 1e-9:
            continue
        x0, x1 = int(max(min(x.min(), W - 1), 0)), int(min(max(x.max(), 0), W - 1))
        y0, y1 = int(max(min(y.min(), H - 1), 0)), int(min(max(y.max(), 0), H - 1))
        if x1 < x0 or y1 < y0:
            continue
        px, py = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        w0 = ((x[1] - px) * (y[2] - py) - (x[2] - px) * (y[1] - py)) / area
        w1 = ((x[2] - px) * (y[0] - py) - (x[0] - px) * (y[2] - py)) / area
        w2 = 1 - w0 - w1
        inside = (w0 >= -1e-6) & (w1 >= -1e-6) & (w2 >= -1e-6)
        if not inside.any():
            continue
        iz = w0 / z[0] + w1 / z[1] + w2 / z[2]
        depth = 1 / iz
        sub = zbuf[y0:y1 + 1, x0:x1 + 1]
        vis = inside & (depth < sub - 1e-5)
        if not vis.any():
            continue
        n = wn[t].mean(0)
        n /= max(np.linalg.norm(n), 1e-8)
        to_eye = -(wp[t].mean(0) - (wp[t].mean(0) - (wp[t].mean(0) - 0)))  # unused
        lam = max(np.dot(n, L), 0)
        shade = 0.35 + 0.75 * lam
        col = np.broadcast_to(lin2srgb(base * (0.6 + 0.4 * (1 - metallic)) * shade + metallic * 0.12 * shade), vis.shape + (3,))
        a = np.full(vis.shape, alpha)
        if texarr is not None:
            u = (w0 * uv[t[0], 0] / z[0] + w1 * uv[t[1], 0] / z[1] + w2 * uv[t[2], 0] / z[2]) * depth
            v = (w0 * uv[t[0], 1] / z[0] + w1 * uv[t[1], 1] / z[1] + w2 * uv[t[2], 1] / z[2]) * depth
            th, tw = texarr.shape[:2]
            tx = (np.mod(u, 1.0) * tw).astype(int).clip(0, tw - 1)
            ty = (np.mod(v, 1.0) * th).astype(int).clip(0, th - 1)
            s = texarr[ty, tx]
            col = s[..., :3] * min(shade, 1.1)
            a = s[..., 3] * alpha
            if mode == "MASK":
                vis = vis & (a >= 0.5)
                a = np.ones_like(a)
        region = img[y0:y1 + 1, x0:x1 + 1]
        if blend or mode == "BLEND":
            region[vis] = region[vis] * (1 - a[vis, None]) + col[vis] * a[vis, None]
        else:
            region[vis] = col[vis]
            sub[vis] = depth[vis]


def frame(prims, direction=(0.55, 0.35, 1.0), margin=1.25, fov=35):
    pts = np.vstack([p[0] for p in prims])
    lo, hi = pts.min(0), pts.max(0)
    c = (lo + hi) / 2
    r = np.linalg.norm(hi - lo) / 2
    d = np.array(direction, float)
    d /= np.linalg.norm(d)
    dist = r * margin / math.tan(math.radians(fov) / 2)
    return c + d * dist, c


def rot_local(g, name, axis, deg):
    n = next(n for n in g["nodes"] if n["name"] == name)
    m = node_matrix(n)
    a = math.radians(deg)
    ax = np.array(axis, float)
    q = [*(ax * math.sin(a / 2)), math.cos(a / 2)]
    r = np.eye(4)
    r[:3, :3] = quat_mat(q)
    return {name: m @ r}


if __name__ == "__main__":
    models, out = sys.argv[1], sys.argv[2]
    import glob
    import os
    os.makedirs(out, exist_ok=True)
    report = {}
    for path in sorted(glob.glob(models + "/*.glb")):
        g, b = load_glb(path)
        stats, imgs = validate(g, b)
        report[os.path.basename(path)] = stats
        print("  OK", os.path.basename(path), stats)
    json.dump(report, open(out + "/_statistiche.json", "w"), indent=2)
