"""Genera tutte le texture PNG del VR Procedure Trainer.

Ogni texture viene disegnata a risoluzione doppia e poi ridotta, per avere bordi
antialiasing. I cartelli sono illustrativi: forme e colori seguono le convenzioni
della segnaletica di sicurezza, ma non sono pittogrammi ISO 7010 certificati.
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1] if len(sys.argv) > 1 else "out/Textures"
os.makedirs(OUT, exist_ok=True)

FONT_DIR = "/usr/share/fonts/truetype/dejavu/"
F_BOLD = FONT_DIR + "DejaVuSans-Bold.ttf"
F_COND = FONT_DIR + "DejaVuSansCondensed-Bold.ttf"
F_REG = FONT_DIR + "DejaVuSans.ttf"

SS = 2  # supersampling

YELLOW = (249, 168, 0)
BLACK = (20, 20, 20)
WHITE = (250, 250, 248)
RED = (204, 6, 5)
GREEN = (0, 150, 70)
BLUE = (0, 90, 170)
PLATE = (222, 224, 222)


def font(path, size):
    return ImageFont.truetype(path, size * SS)


def canvas(w, h, color=(0, 0, 0, 0)):
    img = Image.new("RGBA", (w * SS, h * SS), color)
    return img, ImageDraw.Draw(img)


def save(img, name):
    w, h = img.size
    img = img.resize((w // SS, h // SS), Image.LANCZOS)
    img.save(os.path.join(OUT, name), optimize=True)
    print("  ", name, img.size)


def fit_font(draw, text, path, max_w, max_h):
    """Restituisce il font più grande (in punti logici) che sta nel riquadro."""
    size = max_h
    while size > 6:
        f = font(path, size)
        l, t, r, b = draw.textbbox((0, 0), text, font=f)
        if r - l <= max_w * SS and b - t <= max_h * SS:
            return f
        size -= 1
    return font(path, 6)


def centered_text(draw, text, cx, cy, f, fill):
    draw.text((cx * SS, cy * SS), text, font=f, fill=fill, anchor="mm")


def multiline_block(draw, lines, box, path, fill, spacing=0.18):
    """Scrive righe centrate dentro box=(x0,y0,x1,y1), stessa dimensione di font."""
    x0, y0, x1, y1 = box
    n = len(lines)
    line_h = (y1 - y0) / (n + (n - 1) * spacing)
    f = None
    for ln in lines:
        cand = fit_font(draw, ln, path, (x1 - x0) * 0.92, line_h * 0.9)
        if f is None or cand.size < f.size:
            f = cand
    cy = y0 + line_h / 2
    for ln in lines:
        centered_text(draw, ln, (x0 + x1) / 2, cy, f, fill)
        cy += line_h * (1 + spacing)


def s(v):
    """Coordinate logiche -> pixel supersampled."""
    if isinstance(v, (list, tuple)):
        if v and isinstance(v[0], (list, tuple)):
            return [(x * SS, y * SS) for x, y in v]
        return [x * SS for x in v]
    return int(round(v * SS))


# Etichette dei comandi (targhette incise, testo nero su alluminio chiaro)

def legend_plate(name, text, w=512, h=128):
    img, d = canvas(w, h)
    d.rounded_rectangle(s((2, 2, w - 2, h - 2)), radius=s(14), fill=PLATE, outline=(120, 122, 120), width=s(4))
    f = fit_font(d, text, F_COND, w * 0.86, h * 0.62)
    centered_text(d, text, w / 2, h / 2 + 2, f, BLACK)
    save(img, name)


# Tasti dei pulsanti (simboli IEC: I = avvio, O = arresto)

def button_cap(name, color, symbol):
    w = h = 256
    img, d = canvas(w, h, color + (255,))
    cx = cy = w / 2
    # Anello piu' scuro sul bordo: il tasto viene mappato su un disco inscritto nel quadrato
    dark = tuple(int(c * 0.72) for c in color)
    d.ellipse(s((6, 6, w - 6, h - 6)), outline=dark, width=s(16))
    if symbol == "I":
        d.rounded_rectangle(s((cx - 13, cy - 62, cx + 13, cy + 62)), radius=s(6), fill=WHITE)
    else:
        d.ellipse(s((cx - 55, cy - 55, cx + 55, cy + 55)), outline=WHITE, width=s(24))
    save(img, name)


# Placca gialla dell'arresto di emergenza con testo circolare

def text_on_arc(img, text, cx, cy, radius, f, fill, top=True):
    d = ImageDraw.Draw(img)
    widths = [d.textlength(ch, font=f) for ch in text]
    total = sum(widths) + (len(text) - 1) * 2 * SS
    angle_span = total / (radius * SS)
    a = -math.pi / 2 - angle_span / 2 if top else math.pi / 2 + angle_span / 2
    for ch, wch in zip(text, widths):
        step = (wch + 2 * SS) / (radius * SS)
        mid = a + (step / 2 if top else -step / 2)
        x = cx * SS + radius * SS * math.cos(mid)
        y = cy * SS + radius * SS * math.sin(mid)
        tile = Image.new("RGBA", (int(f.size * 1.6), int(f.size * 1.6)), (0, 0, 0, 0))
        td = ImageDraw.Draw(tile)
        td.text((tile.width / 2, tile.height / 2), ch, font=f, fill=fill, anchor="mm")
        rot = -math.degrees(mid) - 90 if top else -math.degrees(mid) + 90
        tile = tile.rotate(rot, resample=Image.BICUBIC)
        img.alpha_composite(tile, (int(x - tile.width / 2), int(y - tile.height / 2)))
        a += step if top else -step


def emergency_plate():
    w = h = 1024
    img, d = canvas(w, h)
    c = w / 2
    d.ellipse(s((8, 8, w - 8, h - 8)), fill=YELLOW, outline=(180, 120, 0), width=s(6))
    # Zona centrale dove si monta la ghiera del fungo
    d.ellipse(s((c - 250, c - 250, c + 250, c + 250)), fill=(235, 158, 0))
    f = font(F_BOLD, 74)
    text_on_arc(img, "EMERGENCY STOP", c, c, 375, f, BLACK, top=True)
    text_on_arc(img, "PUSH TO STOP", c, c, 375, font(F_BOLD, 62), BLACK, top=False)
    save(img, "Placca_Emergenza.png")


# Placca del sezionatore rotativo: 0 (OFF) a sinistra, I (ON) in alto

def isolator_plate():
    w = h = 512
    img, d = canvas(w, h, YELLOW + (255,))
    c = w / 2
    d.rectangle(s((0, 0, w - 1, h - 1)), outline=(170, 110, 0), width=s(8))
    # Viti agli angoli
    for (x, y) in [(36, 36), (w - 36, 36), (36, h - 36), (w - 36, h - 36)]:
        d.ellipse(s((x - 12, y - 12, x + 12, y + 12)), fill=(150, 150, 150), outline=(90, 90, 90), width=s(2))
        d.line(s((x - 8, y, x + 8, y)), fill=(70, 70, 70), width=s(3))
    # Arco che collega le due posizioni
    d.arc(s((c - 175, c - 175, c + 175, c + 175)), start=180, end=270, fill=BLACK, width=s(8))
    f = font(F_BOLD, 72)
    centered_text(d, "I", c, 52, f, BLACK)
    centered_text(d, "ON", c + 72, 52, font(F_BOLD, 34), BLACK)
    centered_text(d, "0", 52, c, f, BLACK)
    centered_text(d, "OFF", 52, c + 62, font(F_BOLD, 34), BLACK)
    multiline_block(d, ["MAIN", "SWITCH"], (150, 400, 470, 480), F_BOLD, BLACK)
    save(img, "Placca_Sezionatore.png")


# Targhe e intestazioni

def nameplate_cabinet():
    w, h = 1024, 128
    img, d = canvas(w, h)
    d.rounded_rectangle(s((2, 2, w - 2, h - 2)), radius=s(10), fill=(40, 44, 48), outline=(140, 140, 140), width=s(4))
    f = fit_font(d, "CONTROL PANEL  ·  PRESS P-01", F_BOLD, w * 0.9, h * 0.55)
    centered_text(d, "CONTROL PANEL  ·  PRESS P-01", w / 2, h / 2 + 2, f, WHITE)
    save(img, "Targa_Quadro.png")


def nameplate_machine():
    w, h = 512, 256
    img, d = canvas(w, h)
    d.rounded_rectangle(s((2, 2, w - 2, h - 2)), radius=s(12), fill=(205, 207, 205), outline=(100, 100, 100), width=s(4))
    for (x, y) in [(22, 22), (w - 22, 22), (22, h - 22), (w - 22, h - 22)]:
        d.ellipse(s((x - 8, y - 8, x + 8, y + 8)), fill=(140, 140, 140))
    d.rectangle(s((40, 22, w - 40, 72)), fill=(40, 44, 48))
    f = fit_font(d, "MECHANICAL PRESS PE-60", F_BOLD, w - 100, 34)
    centered_text(d, "MECHANICAL PRESS PE-60", w / 2, 47, f, WHITE)
    rows = [("Serial No.", "P-01 / 0001"), ("Year", "2026"), ("Voltage", "400 V  50 Hz"),
            ("Rated force", "600 kN"), ("Use", "Demo (VR)")]
    fl = font(F_REG, 20)
    fv = font(F_BOLD, 20)
    y = 92
    for k, v in rows:
        d.text(s((44, y)), k, font=fl, fill=BLACK, anchor="lm")
        d.text(s((w - 44, y)), v, font=fv, fill=BLACK, anchor="rm")
        d.line(s((44, y + 15, w - 44, y + 15)), fill=(160, 160, 160), width=s(1))
        y += 31
    save(img, "Targa_Pressa.png")


def station_header():
    w, h = 1024, 256
    img, d = canvas(w, h, (230, 30, 30, 255))
    d.rectangle(s((0, 0, w - 1, h - 1)), outline=WHITE, width=s(10))
    multiline_block(d, ["LOTO STATION"], (40, 24, w - 40, 150), F_BOLD, WHITE)
    multiline_block(d, ["One lock per worker"], (60, 160, w - 60, 232), F_COND, WHITE)
    save(img, "Intestazione_StazioneLOTO.png")


# Pittogrammi (disegnati da zero, solo forme semplici)

def draw_bolt(d, cx, cy, size, fill):
    p = [(0.10, -0.50), (-0.22, 0.05), (0.00, 0.05), (-0.12, 0.50), (0.25, -0.10), (0.03, -0.10), (0.18, -0.50)]
    d.polygon([s((cx + x * size, cy + y * size)) for x, y in p], fill=fill)


def gear_points(cx, cy, r_out, r_in, teeth):
    pts = []
    for i in range(teeth * 4):
        a = 2 * math.pi * i / (teeth * 4)
        r = r_out if (i % 4) in (1, 2) else r_in
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def draw_gears(d, cx, cy, size, fill, hole):
    g1 = (cx - size * 0.17, cy + size * 0.08, size * 0.30)
    g2 = (cx + size * 0.24, cy - size * 0.14, size * 0.20)
    for (x, y, r), teeth in [(g1, 10), (g2, 8)]:
        d.polygon([s(p) for p in gear_points(x, y, r, r * 0.78, teeth)], fill=fill)
        d.ellipse(s((x - r * 0.28, y - r * 0.28, x + r * 0.28, y + r * 0.28)), fill=hole)


def draw_padlock_icon(d, cx, cy, size, fill, bg):
    bw, bh = size * 0.56, size * 0.42
    d.rounded_rectangle(s((cx - bw / 2, cy - bh * 0.05, cx + bw / 2, cy + bh * 0.95)), radius=s(size * 0.06), fill=fill)
    r = size * 0.19
    d.arc(s((cx - r, cy - bh * 0.05 - r * 1.9, cx + r, cy - bh * 0.05 + r * 0.1)), start=180, end=360, fill=fill, width=s(size * 0.08))
    d.rectangle(s((cx - r - size * 0.04, cy - bh * 0.05 - r * 0.9, cx - r + size * 0.04, cy)), fill=fill)
    d.rectangle(s((cx + r - size * 0.04, cy - bh * 0.05 - r * 0.9, cx + r + size * 0.04, cy)), fill=fill)
    d.ellipse(s((cx - size * 0.05, cy + bh * 0.3, cx + size * 0.05, cy + bh * 0.5)), fill=bg)
    d.rectangle(s((cx - size * 0.02, cy + bh * 0.45, cx + size * 0.02, cy + bh * 0.7)), fill=bg)


def warning_sign(name, lines, pictogram):
    w, h = 512, 640
    img, d = canvas(w, h, WHITE + (255,))
    d.rectangle(s((0, 0, w - 1, h - 1)), outline=BLACK, width=s(6))
    cx, top, side = w / 2, 40, 400
    hgt = side * math.sqrt(3) / 2
    tri = [(cx, top), (cx - side / 2, top + hgt), (cx + side / 2, top + hgt)]
    d.polygon([s(p) for p in tri], fill=BLACK)
    inset = 24
    tri_in = [(cx, top + inset * 2), (cx - side / 2 + inset * 1.73, top + hgt - inset), (cx + side / 2 - inset * 1.73, top + hgt - inset)]
    d.polygon([s(p) for p in tri_in], fill=YELLOW)
    pictogram(d, cx, top + hgt * 0.64, 190)
    d.line(s((20, 430, w - 20, 430)), fill=BLACK, width=s(4))
    multiline_block(d, lines, (30, 450, w - 30, 615), F_BOLD, BLACK)
    save(img, name)


def mandatory_sign(name, lines):
    w, h = 512, 640
    img, d = canvas(w, h, WHITE + (255,))
    d.rectangle(s((0, 0, w - 1, h - 1)), outline=BLACK, width=s(6))
    cx, cy, r = w / 2, 215, 180
    d.ellipse(s((cx - r, cy - r, cx + r, cy + r)), fill=BLUE)
    draw_padlock_icon(d, cx, cy - 10, 260, WHITE, BLUE)
    d.line(s((20, 430, w - 20, 430)), fill=BLACK, width=s(4))
    multiline_block(d, lines, (30, 450, w - 30, 615), F_BOLD, BLACK)
    save(img, name)


# Lucchetto e cartellino LOTO

def padlock_label():
    w = h = 256
    img, d = canvas(w, h, (205, 20, 20, 255))
    d.rectangle(s((10, 10, w - 10, h - 10)), outline=WHITE, width=s(6))
    multiline_block(d, ["LOTO"], (20, 30, w - 20, 120), F_BOLD, WHITE)
    multiline_block(d, ["DANGER", "DO NOT REMOVE"], (24, 135, w - 24, 225), F_COND, WHITE)
    save(img, "Etichetta_Lucchetto.png")


def loto_tag():
    w, h = 512, 1024
    img, d = canvas(w, h, WHITE + (255,))
    d.rectangle(s((0, 0, w - 1, h - 1)), outline=BLACK, width=s(6))
    d.rectangle(s((6, 150, w - 6, 380)), fill=RED)
    d.ellipse(s((w / 2 - 34, 46, w / 2 + 34, 114)), fill=(60, 60, 60))
    d.ellipse(s((w / 2 - 24, 56, w / 2 + 24, 104)), fill=(25, 25, 25))
    multiline_block(d, ["DANGER"], (30, 180, w - 30, 350), F_BOLD, WHITE)
    multiline_block(d, ["DO NOT", "OPERATE"], (30, 410, w - 30, 600), F_BOLD, BLACK)
    multiline_block(d, ["Equipment under maintenance"], (30, 620, w - 30, 670), F_COND, BLACK)
    fl = font(F_REG, 30)
    for i, lab in enumerate(["Name:", "Dept.:", "Date:"]):
        y = 740 + i * 80
        d.text(s((40, y)), lab, font=fl, fill=BLACK, anchor="ls")
        d.line(s((175, y + 4, w - 40, y + 4)), fill=BLACK, width=s(3))
    save(img, "Cartellino_LOTO.png")


# Quadrante del manometro: 0–10 bar su 270 gradi

def gauge_dial():
    w = h = 1024
    img, d = canvas(w, h)
    c = w / 2
    d.ellipse(s((6, 6, w - 6, h - 6)), fill=WHITE, outline=(60, 60, 60), width=s(10))

    def polar(v, r):
        a = math.radians(v * 27 - 135)
        return (c + r * math.sin(a), c - r * math.cos(a))

    # Zona rossa oltre gli 8 bar
    d.arc(s((c - 400, c - 400, c + 400, c + 400)), start=-90 + (8 * 27 - 135), end=-90 + 135, fill=RED, width=s(26))
    for i in range(0, 101):
        v = i / 10
        major = i % 10 == 0
        half = i % 5 == 0
        r0 = 330 if major else (355 if half else 370)
        d.line(s(polar(v, r0) + polar(v, 405)), fill=BLACK, width=s(10 if major else (6 if half else 3)))
    f = font(F_BOLD, 68)
    for v in range(11):
        x, y = polar(v, 260)
        centered_text(d, str(v), x, y, f, BLACK)
    centered_text(d, "bar", c, c + 190, font(F_BOLD, 60), BLACK)
    centered_text(d, "COMPRESSED AIR", c, c + 280, font(F_COND, 40), (80, 80, 80))
    save(img, "Manometro_Quadrante.png")


# Strisce di pericolo (giallo/nero a 45°, ripetibili)

def hazard_stripes():
    w = h = 512
    period = 128
    img = Image.new("RGBA", (w, h))
    px = img.load()
    for y in range(h):
        for x in range(w):
            px[x, y] = (YELLOW if ((x + y) // (period // 2)) % 2 == 0 else BLACK) + (255,)
    img.save(os.path.join(OUT, "Strisce_Pericolo.png"), optimize=True)
    print("   Strisce_Pericolo.png", img.size)


# Elementi per i pannelli UI nello spazio 3D

def ui_panel():
    w = h = 256
    img, d = canvas(w, h)
    d.rounded_rectangle(s((4, 4, w - 4, h - 4)), radius=s(40), fill=(18, 24, 30, 225), outline=(249, 168, 0, 255), width=s(6))
    save(img, "UI_Pannello.png")


def ui_icon(name, color, kind):
    w = h = 256
    img, d = canvas(w, h)
    c = w / 2
    if kind == "warn":
        d.polygon(s([(c, 18), (14, 232), (242, 232)]), fill=color)
        d.rounded_rectangle(s((c - 14, 82, c + 14, 172)), radius=s(8), fill=BLACK)
        d.ellipse(s((c - 16, 186, c + 16, 218)), fill=BLACK)
    else:
        d.ellipse(s((10, 10, w - 10, h - 10)), fill=color)
        if kind == "ok":
            d.line(s((70, 132, 112, 176, 190, 88)), fill=WHITE, width=s(26), joint="curve")
        elif kind == "err":
            d.line(s((82, 82, 174, 174)), fill=WHITE, width=s(26))
            d.line(s((174, 82, 82, 174)), fill=WHITE, width=s(26))
        elif kind == "info":
            d.ellipse(s((c - 17, 52, c + 17, 86)), fill=WHITE)
            d.rounded_rectangle(s((c - 15, 106, c + 15, 206)), radius=s(8), fill=WHITE)
    save(img, name)


def ui_arrow():
    w, h = 256, 256
    img, d = canvas(w, h)
    c = w / 2
    pts = [(c - 44, 14), (c + 44, 14), (c + 44, 120), (c + 100, 120), (c, 242), (c - 100, 120), (c - 44, 120)]
    d.polygon(s(pts), fill=YELLOW, outline=BLACK)
    d.line(s(pts + [pts[0]]), fill=BLACK, width=s(8), joint="curve")
    save(img, "UI_Freccia.png")


if __name__ == "__main__":
    print("Texture ->", OUT)
    legend_plate("Etichetta_Start.png", "START")
    legend_plate("Etichetta_Stop.png", "STOP")
    legend_plate("Etichetta_TensionePresente.png", "POWER ON")
    legend_plate("Etichetta_MacchinaInMoto.png", "MACHINE RUNNING")
    legend_plate("Etichetta_Voltmetro.png", "VOLTMETER")
    legend_plate("Etichetta_ScaricoAria.png", "AIR RELEASE")
    button_cap("Tasto_Start.png", GREEN, "I")
    button_cap("Tasto_Stop.png", RED, "O")
    emergency_plate()
    isolator_plate()
    nameplate_cabinet()
    nameplate_machine()
    station_header()
    warning_sign("Cartello_PericoloTensione.png", ["DANGER", "HIGH VOLTAGE"],
                 lambda d, x, y, z: draw_bolt(d, x, y, z, BLACK))
    warning_sign("Cartello_OrganiInMovimento.png", ["WARNING", "MOVING PARTS"],
                 lambda d, x, y, z: draw_gears(d, x, y, z, BLACK, YELLOW))
    mandatory_sign("Cartello_ObbligoLOTO.png", ["ISOLATE AND LOCK OUT", "BEFORE MAINTENANCE"])
    padlock_label()
    loto_tag()
    gauge_dial()
    hazard_stripes()
    ui_panel()
    ui_icon("UI_Icona_Ok.png", GREEN, "ok")
    ui_icon("UI_Icona_Errore.png", RED, "err")
    ui_icon("UI_Icona_Attenzione.png", YELLOW, "warn")
    ui_icon("UI_Icona_Info.png", BLUE, "info")
    ui_arrow()
