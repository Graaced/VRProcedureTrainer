import sys, numpy as np
from preview import load_glb, validate, collect, render, frame, rot_local
M, OUT = sys.argv[1], sys.argv[2]

def prims_of(file, overrides=None, offset=(0,0,0)):
    g,b = load_glb(f"{M}/{file}"); _,imgs = validate(g,b)
    ps = collect(g,b,imgs,overrides)
    off = np.array(offset)
    return [(p[0]+off,)+p[1:] for p in ps], g

# Sezionatore in OFF (glTF +90 su Z) con il lucchetto agganciato al socket
g,_ = load_glb(f"{M}/Sezionatore.glb")
sez, g = prims_of("Sezionatore.glb", rot_local(g, "Maniglia", (0,0,1), 90))
sock = np.array(next(n for n in g["nodes"] if n["name"]=="Socket_Lucchetto")["translation"])
lock,_ = prims_of("Lucchetto_LOTO.glb", offset=sock - np.array([0.012,0.050,0]))
allp = sez+lock
for nm,d in (("Posa_SezionatoreOFF_Lucchetto",(0.25,0.15,1.0)),("Posa_SezionatoreOFF_Lucchetto_Lato",(-1.0,0.6,0.5))):
    eye,tgt = frame(allp, d, 1.3)
    render(allp, eye, tgt, (600,600)).save(f"{OUT}/{nm}.png")

# Carter aperto (glTF -90 su Y) e manometro a 0 bar (glTF +135 su Z)
g,_ = load_glb(f"{M}/Pressa_P01.glb")
ov = rot_local(g,"Carter",(0,1,0),-90); ov.update(rot_local(g,"Lancetta",(0,0,1),135))
ov.update(rot_local(g,"Leva",(0,0,1),90))
pr,_ = prims_of("Pressa_P01.glb", ov)
eye,tgt = frame(pr,(0.3,0.35,1.0),1.15)
render(pr, eye, tgt, (900,800)).save(f"{OUT}/Posa_CarterAperto.png")
render(pr, np.array([-0.56,0.85,0.75]), np.array([-0.56,0.85,0.3]), (500,600), 35).save(f"{OUT}/Posa_Pneumatica.png")
print("ok")
