"""Costruisce i modelli GLB del VR Procedure Trainer.

Ogni parte che si muove è un nodo separato con il pivot nel punto di rotazione o di
traslazione. Le posizioni dei comandi sul quadro sono nodi vuoti "Mount_*" / "Socket_*" /
"Attach_*", da usare come attach transform in Unity.
"""
import json
import os
import sys

from meshkit import (GlbWriter, Node, box, cyl, cylinder, decal, dome, merge, quat_from_axis,
                     rot_x, rot_z, torus_arc, tube)

TEX = sys.argv[1]
OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)

DZ = 0.0006  # distanza delle decal dalla superficie, contro lo z-fighting

MATS = {
    "M_Verniciato_Verde": {"color": (0.13, 0.21, 0.11), "roughness": 0.5},
    "M_Verniciato_Grigio": {"color": (0.60, 0.63, 0.61), "roughness": 0.55},
    "M_Antracite": {"color": (0.035, 0.042, 0.048), "roughness": 0.6},
    "M_Acciaio": {"color": (0.60, 0.60, 0.62), "metallic": 1.0, "roughness": 0.35},
    "M_AcciaioScuro": {"color": (0.18, 0.18, 0.19), "metallic": 1.0, "roughness": 0.5},
    "M_Ottone": {"color": (0.78, 0.52, 0.20), "metallic": 1.0, "roughness": 0.35},
    "M_Giallo": {"color": (0.95, 0.39, 0.0), "roughness": 0.5},
    "M_Rosso": {"color": (0.60, 0.005, 0.004), "roughness": 0.4},
    "M_Verde": {"color": (0.0, 0.30, 0.06), "roughness": 0.4},
    "M_Bianco": {"color": (0.9, 0.9, 0.88), "roughness": 0.5},
    "M_Gomma": {"color": (0.02, 0.02, 0.02), "roughness": 0.85},
    "M_Policarbonato": {"color": (0.75, 0.80, 0.85), "alpha": 0.25, "roughness": 0.05, "double": True},
    "M_Spia_Bianca": {"color": (0.95, 0.90, 0.70), "roughness": 0.2},
    "M_Spia_Verde": {"color": (0.05, 0.60, 0.12), "roughness": 0.2},
    "M_Semaforo_Rosso": {"color": (0.70, 0.02, 0.02), "roughness": 0.2},
    "M_Semaforo_Giallo": {"color": (0.95, 0.50, 0.02), "roughness": 0.2},
    "M_Semaforo_Verde": {"color": (0.03, 0.55, 0.10), "roughness": 0.2},
    "M_Display": {"color": (0.01, 0.02, 0.015), "roughness": 0.15},
}

TEXTURED = ["Etichetta_Start", "Etichetta_Stop", "Etichetta_TensionePresente", "Etichetta_MacchinaInMoto",
            "Etichetta_Voltmetro", "Etichetta_ScaricoAria", "Tasto_Start", "Tasto_Stop", "Placca_Emergenza",
            "Placca_Sezionatore", "Targa_Quadro", "Targa_Pressa", "Intestazione_StazioneLOTO",
            "Cartello_PericoloTensione", "Cartello_OrganiInMovimento", "Cartello_ObbligoLOTO",
            "Etichetta_Lucchetto", "Cartellino_LOTO", "Manometro_Quadrante", "Strisce_Pericolo"]
for t in TEXTURED:
    MATS["M_" + t] = {"texture": t + ".png", "roughness": 0.6}
# Le texture con trasparenza usano alpha cutout
for t in ("Placca_Emergenza", "Manometro_Quadrante"):
    MATS["M_" + t]["mask"] = True


def empty(name, t):
    return Node(name, t=t)


def parts_of(cyl_dict, front, side, back=None):
    out = [(cyl_dict["side"], side), (cyl_dict["front"], front)]
    if "back" in cyl_dict:
        out.append((cyl_dict["back"], back or side))
    return out


# Componenti del quadro (origine = superficie di montaggio, fronte verso +Z)

def pulsante(name, cap_color, cap_texture, recessed):
    """Pulsante a tasto: il nodo 'Tasto' scorre di 5 mm lungo -Z quando premuto."""
    node = Node(name, parts=[(cyl((0, 0, 0.004), 0.021, 0.008), "M_AcciaioScuro")])
    if recessed:
        # Ghiera alta attorno al tasto di START: evita avvii accidentali
        node.parts.append((tube((0, 0, 0.014), 0.021, 0.0175, 0.012), "M_AcciaioScuro"))
    length = 0.010 if recessed else 0.016
    cap = cylinder((0, 0, length / 2), 0.0155, length)
    node.add(Node("Tasto", t=(0, 0, 0.008), parts=parts_of(cap, cap_texture, cap_color),
                  extras={"corsa_m": 0.005, "asse": "-Z locale"}))
    return node


def pulsante_emergenza():
    """Fungo di emergenza: il nodo 'Fungo' scorre di 6 mm lungo -Z e resta premuto (ritenuta)."""
    plate = cylinder((0, 0, 0.001), 0.045, 0.002, seg=48)
    node = Node("Pulsante_Emergenza", parts=parts_of(plate, "M_Placca_Emergenza", "M_Giallo") +
                [(cyl((0, 0, 0.008), 0.024, 0.012), "M_AcciaioScuro")])
    node.add(Node("Fungo", t=(0, 0, 0.014), parts=[
        (cyl((0, 0, 0.006), 0.012, 0.012), "M_Rosso"),
        (cyl((0, 0, 0.015), 0.020, 0.006), "M_Rosso"),
        (dome((0, 0, 0.018), 0.020, 0.008), "M_Rosso"),
    ], extras={"corsa_m": 0.006, "asse": "-Z locale", "ritenuta": True}))
    return node


def spia(name, lens_mat):
    """Spia luminosa: la 'Lente' ha un materiale suo, da far emettere a runtime."""
    node = Node(name, parts=[(cyl((0, 0, 0.004), 0.016, 0.008), "M_AcciaioScuro")])
    node.add(Node("Lente", t=(0, 0, 0.008), parts=[
        (cyl((0, 0, 0.002), 0.011, 0.004), lens_mat),
        (dome((0, 0, 0.004), 0.011, 0.006), lens_mat),
    ]))
    return node


def display_voltmetro():
    node = Node("Display_Voltmetro", parts=[
        (box((0, 0, 0.015), (0.14, 0.08, 0.03)), "M_Antracite"),
        (decal((0, 0.005, 0.03 + DZ), (0.11, 0.045)), "M_Display"),
    ])
    node.add(empty("Attach_Testo", (0, 0.005, 0.031)))
    return node


def sezionatore():
    """Sezionatore rotativo. 'Maniglia' ruota attorno a Z locale: ON = 0° (verso l'alto),
    OFF = maniglia verso sinistra. In OFF la linguetta della maniglia si sovrappone a quella
    fissa e i due fori si allineano su 'Socket_Lucchetto'."""
    node = Node("Sezionatore", parts=[
        (box((0, 0, 0.002), (0.12, 0.12, 0.004)), "M_Giallo"),
        (decal((0, 0, 0.004 + DZ), (0.12, 0.12)), "M_Placca_Sezionatore"),
        (cyl((0, 0, 0.009), 0.026, 0.010), "M_AcciaioScuro"),
        # Linguetta fissa con il foro per il lucchetto
        (box((-0.0675, -0.0045, 0.022), (0.035, 0.003, 0.036)), "M_AcciaioScuro"),
        (cyl((-0.072, -0.0045, 0.030), 0.0035, 0.0034, "y", seg=16), "M_Gomma"),
    ])
    node.add(Node("Maniglia", t=(0, 0, 0.014), parts=[
        (cyl((0, 0, 0.006), 0.020, 0.012), "M_Rosso"),
        (box((0, 0.03375, 0.012), (0.022, 0.1085, 0.016)), "M_Rosso"),
        (box((0, 0.075, 0.0205), (0.006, 0.014, 0.001)), "M_Bianco"),
        # Linguetta mobile: in OFF diventa orizzontale sopra quella fissa
        (box((0, 0.0675, 0.008), (0.003, 0.035, 0.036)), "M_AcciaioScuro"),
        (cyl((0, 0.072, 0.016), 0.0035, 0.0034, "x", seg=16), "M_Gomma"),
    ], extras={"ON_gradi": 0, "OFF": "maniglia verso sinistra (Unity/glTFast: Z = -90)"}))
    node.add(empty("Socket_Lucchetto", (-0.072, 0.0, 0.030)))
    return node


# Oggetti afferrabili

def lucchetto():
    """Lucchetto LOTO. Origine al centro del corpo, faccia verso +Z.
    Attach_Socket: punto della gamba destra che entra nel foro del sezionatore."""
    node = Node("Lucchetto_LOTO", parts=[
        (box((0, 0, 0), (0.040, 0.045, 0.020)), "M_Rosso"),
        (decal((0, 0.0, 0.010 + DZ), (0.032, 0.032)), "M_Etichetta_Lucchetto"),
        (cyl((0, -0.0232, 0), 0.005, 0.0015, "y", seg=16), "M_AcciaioScuro"),
        (cyl((-0.012, 0.039, 0), 0.003, 0.033, "y", seg=12), "M_Acciaio"),
        (cyl((0.012, 0.039, 0), 0.003, 0.033, "y", seg=12), "M_Acciaio"),
        (torus_arc((0, 0.0555, 0), 0.012, 0.003, 0, 180), "M_Acciaio"),
    ])
    node.add(empty("Attach_Presa", (0, 0, 0)),
             empty("Attach_Socket", (0.012, 0.050, 0)),
             empty("Attach_Gancio", (0, 0.0645, 0)))
    return node


def cartellino():
    """Cartellino LOTO. Origine nel foro in alto."""
    node = Node("Cartellino_LOTO", parts=[
        (box((0, -0.07, 0), (0.075, 0.14, 0.0012)), "M_Bianco"),
        (decal((0, -0.07, 0.0006 + DZ), (0.075, 0.14)), "M_Cartellino_LOTO"),
    ])
    node.add(empty("Attach_Gancio", (0, 0, 0)), empty("Attach_Presa", (0, -0.08, 0)))
    return node


# Componenti pneumatici

def leva_valvola():
    """Valvola di scarico aria. 'Leva' ruota attorno a Z locale:
    0° (verso l'alto, parallela al tubo) = IN PRESSIONE, ±90° = SCARICO."""
    node = Node("LevaValvola", parts=[
        (box((0, 0, 0.025), (0.08, 0.08, 0.05)), "M_Ottone"),
        (cyl((0, -0.24, 0.025), 0.012, 0.40, "y"), "M_Acciaio"),
        (cyl((0.05, 0, 0.025), 0.010, 0.02, "x", seg=16), "M_AcciaioScuro"),
    ])
    node.add(Node("Leva", t=(0, 0, 0.05), parts=[
        (cyl((0, 0, 0.006), 0.008, 0.012), "M_AcciaioScuro"),
        (box((0, 0.06, 0.016), (0.02, 0.13, 0.008)), "M_Rosso"),
    ], extras={"IN_PRESSIONE_gradi": 0, "SCARICO_gradi": 90}))
    return node


def manometro():
    """Manometro 0–10 bar. 'Lancetta' ruota attorno a Z locale; a 0° indica 5 bar."""
    case = cylinder((0, 0, 0.015), 0.05, 0.03, seg=48)
    node = Node("Manometro", parts=parts_of(case, "M_Manometro_Quadrante", "M_AcciaioScuro") +
                [(tube((0, 0, 0.032), 0.053, 0.047, 0.004, seg=48), "M_Acciaio")])
    node.add(Node("Lancetta", t=(0, 0, 0.031), parts=[
        (box((0, 0.016, 0.001), (0.004, 0.042, 0.002)), "M_Rosso"),
        (cyl((0, 0, 0.0015), 0.004, 0.003, seg=16), "M_AcciaioScuro"),
    ], extras={"formula_unity_gradi_Z": "27 * bar - 135 (verificare il segno)"}))
    return node


# Macchinario

def pressa():
    """Pressa eccentrica. Origine a terra al centro, fronte verso +Z, operatore davanti."""
    root = Node("Pressa_P01")
    corpo = Node("Corpo", parts=[
        (box((0, 0.1, 0), (1.3, 0.2, 1.0)), "M_Antracite"),
        (box((0, 0.55, -0.05), (0.9, 0.7, 0.8)), "M_Verniciato_Verde"),
        (box((0, 0.94, 0.0), (0.86, 0.08, 0.66)), "M_AcciaioScuro"),
        (box((-0.56, 1.075, -0.05), (0.22, 1.75, 0.7)), "M_Verniciato_Verde"),
        (box((0.56, 1.075, -0.05), (0.22, 1.75, 0.7)), "M_Verniciato_Verde"),
        (box((0, 2.2, -0.05), (1.34, 0.5, 0.85)), "M_Verniciato_Verde"),
        (box((0, 1.04, 0.0), (0.6, 0.12, 0.45)), "M_AcciaioScuro"),
        (cyl((0.74, 2.2, -0.1), 0.045, 0.16, "x"), "M_Acciaio"),
        # Motore sopra la testata
        (box((0.1, 2.47, -0.25), (0.30, 0.04, 0.25)), "M_Antracite"),
        (cyl((0.1, 2.64, -0.25), 0.15, 0.42, "x"), "M_Antracite"),
        (cyl((0.335, 2.64, -0.25), 0.12, 0.05, "x"), "M_AcciaioScuro"),
        # Interruttore di sicurezza del carter
        (box((0.50, 1.80, 0.33), (0.05, 0.08, 0.05)), "M_Giallo"),
        # Targa, cartello, strisce di pericolo
        (decal((0.22, 0.55, 0.35 + DZ), (0.24, 0.12)), "M_Targa_Pressa"),
        (decal((-0.56, 1.45, 0.30 + DZ), (0.18, 0.225)), "M_Cartello_OrganiInMovimento"),
        (decal((0, 0.1, 0.5 + DZ), (1.3, 0.08), (8.125, 0.5)), "M_Strisce_Pericolo"),
        (decal((0, 0.94, 0.33 + DZ), (0.86, 0.08), (5.375, 0.5)), "M_Strisce_Pericolo"),
        (decal((-0.56, 0.79, 0.30 + DZ), (0.12, 0.03)), "M_Etichetta_ScaricoAria"),
    ])
    root.add(corpo)

    mazza = Node("Mazza", t=(0, 1.62, 0), parts=[
        (box((0, 0, 0), (0.7, 0.35, 0.55)), "M_Verniciato_Verde"),
        (box((0, -0.225, 0), (0.6, 0.1, 0.45)), "M_AcciaioScuro"),
        (cyl((0, 0.2525, 0), 0.06, 0.155, "y"), "M_Acciaio"),
    ], extras={"nota": "opzionale: animazione su/giù lungo Y con macchina in moto"})
    root.add(mazza)

    # Volano: ruota attorno a X locale; razze e marcatore giallo rendono visibile la rotazione
    spokes = []
    for a in (0, 72, 144, 216, 288):
        g = box((0, 0.21, 0), (0.05, 0.30, 0.04))
        spokes.append(g.transformed(rot_x(a), (0, 0, 0)))
    volano = Node("Volano", t=(0.87, 2.2, -0.1), parts=[
        (tube((0, 0, 0), 0.42, 0.36, 0.10, "x", seg=48), "M_Verniciato_Verde"),
        (cyl((0, 0, 0), 0.08, 0.14, "x"), "M_Acciaio"),
        (merge(spokes), "M_Verniciato_Verde"),
        (box((0, 0.425, 0), (0.10, 0.012, 0.05)), "M_Giallo"),
        (box((0.052, 0.39, 0), (0.006, 0.06, 0.04)), "M_Giallo"),
    ], extras={"asse_rotazione": "X locale"})
    root.add(volano)

    # Carter: pivot sulla cerniera sinistra, ruota attorno a Y locale
    W, H = 0.92, 0.92
    carter = Node("Carter", t=(-0.46, 0.99, 0.325), parts=[
        (box((0.02, H / 2, 0), (0.04, H, 0.03)), "M_Giallo"),
        (box((W - 0.02, H / 2, 0), (0.04, H, 0.03)), "M_Giallo"),
        (box((W / 2, 0.02, 0), (W, 0.04, 0.03)), "M_Giallo"),
        (box((W / 2, H - 0.02, 0), (W, 0.04, 0.03)), "M_Giallo"),
        (box((W / 2, H / 2, 0), (W - 0.08, H - 0.08, 0.006)), "M_Policarbonato"),
        (cyl((0, 0.15, -0.005), 0.012, 0.08, "y"), "M_AcciaioScuro"),
        (cyl((0, 0.77, -0.005), 0.012, 0.08, "y"), "M_AcciaioScuro"),
        (box((W - 0.06, 0.36, 0.03), (0.02, 0.02, 0.03)), "M_AcciaioScuro"),
        (box((W - 0.06, 0.60, 0.03), (0.02, 0.02, 0.03)), "M_AcciaioScuro"),
        (cyl((W - 0.06, 0.48, 0.05), 0.012, 0.30, "y"), "M_Gomma"),
    ], extras={"asse_rotazione": "Y locale", "aperto": "Unity/glTFast: Y = +90"})
    carter.add(empty("Attach_Maniglia", (W - 0.06, 0.48, 0.05)))
    root.add(carter)

    # Semaforo di stato sulla testata
    sem = Node("Semaforo", t=(0.55, 2.45, 0.25), parts=[
        (cyl((0, 0.1, 0), 0.015, 0.2, "y"), "M_Antracite"),
        (cyl((0, 0.385, 0), 0.036, 0.03, "y"), "M_Antracite"),
    ])
    for nm, y in (("Semaforo_Verde", 0.22), ("Semaforo_Giallo", 0.28), ("Semaforo_Rosso", 0.34)):
        sem.add(Node(nm, t=(0, y, 0), parts=[(cyl((0, 0, 0), 0.035, 0.058, "y"), "M_" + nm)]))
    root.add(sem)

    # Pneumatica sulla colonna sinistra
    lv = leva_valvola()
    lv.t = [-0.56, 0.62, 0.30]
    mano = manometro()
    mano.t = [-0.56, 1.00, 0.30]
    root.add(lv, mano)
    return root


def quadro_comandi():
    """Quadro comandi su colonna, con tutti i comandi montati. Origine a terra, fronte +Z."""
    root = Node("QuadroComandi")
    struttura = Node("Struttura", parts=[
        (box((0, 0.01, 0), (0.4, 0.02, 0.35)), "M_Antracite"),
        (box((0, 0.52, -0.03), (0.1, 1.0, 0.1)), "M_Antracite"),
        (cyl((-0.085, 0.52, -0.03), 0.018, 1.0, "y"), "M_Gomma"),
        (box((0, 1.37, 0), (0.6, 0.7, 0.25)), "M_Verniciato_Grigio"),
        (box((0, 1.37, 0.13), (0.57, 0.67, 0.01)), "M_Verniciato_Grigio"),
        (box((0, 1.73, 0.0), (0.64, 0.02, 0.29)), "M_Antracite"),
        (box((0.255, 1.30, 0.143), (0.02, 0.08, 0.016)), "M_Antracite"),
        (cyl((-0.29, 1.55, 0.13), 0.008, 0.06, "y", seg=12), "M_AcciaioScuro"),
        (cyl((-0.29, 1.19, 0.13), 0.008, 0.06, "y", seg=12), "M_AcciaioScuro"),
        (decal((0, 1.685, 0.135 + DZ), (0.40, 0.05)), "M_Targa_Quadro"),
        (decal((0.17, 1.17, 0.135 + DZ), (0.10, 0.125)), "M_Cartello_PericoloTensione"),
        (decal((-0.20, 1.575, 0.135 + DZ), (0.10, 0.025)), "M_Etichetta_TensionePresente"),
        (decal((-0.08, 1.575, 0.135 + DZ), (0.10, 0.025)), "M_Etichetta_MacchinaInMoto"),
        (decal((0.13, 1.565, 0.135 + DZ), (0.10, 0.025)), "M_Etichetta_Voltmetro"),
        (decal((-0.20, 1.395, 0.135 + DZ), (0.08, 0.02)), "M_Etichetta_Start"),
        (decal((-0.08, 1.395, 0.135 + DZ), (0.08, 0.02)), "M_Etichetta_Stop"),
    ])
    root.add(struttura)
    z = 0.135
    items = [
        (spia("Spia_TensionePresente", "M_Spia_Bianca"), (-0.20, 1.62, z)),
        (spia("Spia_MacchinaInMoto", "M_Spia_Verde"), (-0.08, 1.62, z)),
        (display_voltmetro(), (0.13, 1.62, z)),
        (pulsante("Pulsante_Start", "M_Verde", "M_Tasto_Start", recessed=True), (-0.20, 1.44, z)),
        (pulsante("Pulsante_Stop", "M_Rosso", "M_Tasto_Stop", recessed=False), (-0.08, 1.44, z)),
        (pulsante_emergenza(), (0.13, 1.43, z)),
        (sezionatore(), (-0.14, 1.20, z)),
    ]
    for node, pos in items:
        node.t = list(pos)
        root.add(node)
    return root


def stazione_loto():
    """Stazione LOTO su colonna con 4 ganci. Origine a terra, fronte +Z."""
    root = Node("StazioneLOTO", parts=[
        (box((0, 0.01, 0), (0.45, 0.02, 0.35)), "M_Antracite"),
        (box((0, 0.71, -0.05), (0.06, 1.38, 0.06)), "M_Antracite"),
        (box((0, 1.40, 0), (0.6, 0.5, 0.02)), "M_Giallo"),
        (decal((0, 1.57, 0.01 + DZ), (0.56, 0.14)), "M_Intestazione_StazioneLOTO"),
        (decal((0.21, 1.33, 0.01 + DZ), (0.12, 0.15)), "M_Cartello_ObbligoLOTO"),
    ])
    for i, x in enumerate((-0.22, -0.13, -0.04, 0.07), start=1):
        root.parts.append((cyl((x, 1.44, 0.03), 0.004, 0.04, seg=12), "M_Acciaio"))
        root.parts.append((cyl((x, 1.448, 0.048), 0.004, 0.016, "y", seg=12), "M_Acciaio"))
        root.add(empty(f"Gancio_{i}", (x, 1.444, 0.035)))
    return root


def segnaletica_pavimento():
    """Rettangolo di strisce gialle e nere attorno alla pressa (decal a pavimento)."""
    strips = []
    x0, x1, z0, z1, w = -0.95, 0.95, -0.70, 0.80, 0.08
    for (cx, cz, sx, sz) in [((x0 + x1) / 2, z0, x1 - x0 + w, w), ((x0 + x1) / 2, z1, x1 - x0 + w, w),
                              (x0, (z0 + z1) / 2, w, z1 - z0 - w), (x1, (z0 + z1) / 2, w, z1 - z0 - w)]:
        length = max(sx, sz)
        g = decal((0, 0, 0), (length, w), (length / 0.16, 0.5))
        if sz > sx:
            g = g.transformed(rot_z(90), None)
        strips.append(g.transformed(rot_x(-90), (cx, 0.002, cz)))
    return Node("Segnaletica_Pavimento", parts=[(merge(strips), "M_Strisce_Pericolo")])


def write(node, name):
    path = os.path.join(OUT, name)
    GlbWriter(MATS, TEX).write(node, path)
    print("  ", name, os.path.getsize(path) // 1024, "KB")


def place(node, t, yaw=None):
    node.t = list(t)
    if yaw is not None:
        node.r = quat_from_axis((0, 1, 0), yaw)
    return node


if __name__ == "__main__":
    print("Modelli ->", OUT)
    write(pressa(), "Pressa_P01.glb")
    write(quadro_comandi(), "QuadroComandi.glb")
    write(stazione_loto(), "StazioneLOTO.glb")
    write(lucchetto(), "Lucchetto_LOTO.glb")
    write(cartellino(), "Cartellino_LOTO.glb")
    write(pulsante("Pulsante_Start", "M_Verde", "M_Tasto_Start", recessed=True), "Pulsante_Start.glb")
    write(pulsante("Pulsante_Stop", "M_Rosso", "M_Tasto_Stop", recessed=False), "Pulsante_Stop.glb")
    write(pulsante_emergenza(), "Pulsante_Emergenza.glb")
    write(sezionatore(), "Sezionatore.glb")
    write(spia("Spia_TensionePresente", "M_Spia_Bianca"), "Spia_TensionePresente.glb")
    write(spia("Spia_MacchinaInMoto", "M_Spia_Verde"), "Spia_MacchinaInMoto.glb")
    write(display_voltmetro(), "Display_Voltmetro.glb")
    write(leva_valvola(), "LevaValvola.glb")
    write(manometro(), "Manometro.glb")
    write(segnaletica_pavimento(), "Segnaletica_Pavimento.glb")

    # Scena completa: posizioni coerenti tra macchina, quadro, stazione e oggetti appesi
    scena = Node("Scena_VRPT")
    scena.add(place(pressa(), (0, 0, 0)))
    scena.add(segnaletica_pavimento())
    scena.add(place(quadro_comandi(), (1.30, 0, 0.55), yaw=-25))
    stazione = place(stazione_loto(), (-1.45, 0, 0.45), yaw=25)
    scena.add(stazione)
    # Lucchetto e cartellino appesi ai ganci (coordinate locali della stazione)
    lock = lucchetto()
    lock.t = [-0.22, 1.444 - 0.0645, 0.035]
    tag = cartellino()
    tag.t = [0.07, 1.444, 0.035]
    stazione.add(lock, tag)
    scena.add(Node("Punto_Operatore", t=(0.35, 0, 1.55), r=quat_from_axis((0, 1, 0), 180),
                   extras={"nota": "posizione suggerita per XR Origin, guarda verso la pressa"}))
    write(scena, "Scena_VRPT.glb")
