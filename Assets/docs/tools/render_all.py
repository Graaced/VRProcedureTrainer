import sys, os, numpy as np
from preview import load_glb, validate, collect, render, frame, rot_local
M, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)

def shot(name, file, direction=(0.55,0.35,1.0), overrides=None, size=(900,700), margin=1.25, eye=None, target=None, fov=35):
    g,b = load_glb(f"{M}/{file}"); _,imgs = validate(g,b)
    prims = collect(g,b,imgs,overrides)
    if eye is None:
        eye,target = frame(prims, direction, margin, fov)
    render(prims, eye, target, size, fov).save(f"{OUT}/{name}.png"); print(name)

which = sys.argv[3] if len(sys.argv)>3 else "all"
if which in ("all","scene"):
    shot("Scena_VRPT", "Scena_VRPT.glb", (0.35,0.45,1.0), margin=0.75, size=(1200,800))
if which in ("all","models"):
    for f in ["Pressa_P01","QuadroComandi","StazioneLOTO"]:
        shot(f, f+".glb")
    shot("QuadroComandi_Fronte", "QuadroComandi.glb", eye=(0,1.42,1.05), target=(0,1.40,0), fov=32)
    for f in ["Lucchetto_LOTO","Sezionatore","Pulsante_Emergenza","Pulsante_Start","Manometro","LevaValvola","Cartellino_LOTO"]:
        shot(f, f+".glb", (0.4,0.3,1.0), size=(500,500), margin=1.4)
