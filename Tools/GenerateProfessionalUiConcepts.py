from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "Assets" / "SurvivorFarm" / "Art" / "Sprites" / "FarmRPGTinyAssetPack"
OUT = ROOT / "Assets" / "SurvivorFarm" / "Art" / "UIConcepts" / "ProfessionalV2"


CANVAS = (1600, 900)
SCALE = Image.Resampling.NEAREST


def load(rel):
    return Image.open(PACK / rel).convert("RGBA")


def crop(img, x, y, w, h):
    return img.crop((x, y, x + w, y + h))


def cell(img, col, row, size=16):
    return crop(img, col * size, row * size, size, size)


def font(size, bold=False):
    choices = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/trebucbd.ttf" if bold else "C:/Windows/Fonts/trebuc.ttf",
        "C:/Windows/Fonts/verdana.ttf",
    ]
    for path in choices:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


FONT_TITLE = font(34, True)
FONT_H1 = font(30, True)
FONT_H2 = font(22, True)
FONT_BODY = font(18, True)
FONT_REG = font(16, False)
FONT_SMALL = font(13, True)


ui_hud = load("UI/HUD.png")
ui_bars = load("UI/Bars.png")
ui_clock = load("UI/Clock/Clock.png")
ui_weather = load("UI/weather icons.png")
ui_book = load("UI/Inventory/Book.png")
ui_decor = load("UI/Inventory/Decor.png")
ui_extras = load("UI/Inventory/Extras.png")
weapons = load("Icons/Weapons/RPG/2.png")
food = load("Icons/Food 0.1.png")
crops = load("Icons/Crops.png")
carrot_growth = load("Farm Crops/Spring/Carrot.png")
spring_crops = load("Farm Crops/Spring/Spring Crops.png")
tilled = load("Farm and Tileset/Tileset/Tilled Soil and wet soil.png")
grass_tiles = load("Farm and Tileset/Tileset/Tileset Grass Summer.png")
path_tiles = load("Farm and Tileset/Tileset/Path tiles.png")
barn_sheet = load("Exterior/Houses/Farm Buildings/Barn/Barn.png")
fence = load("Exterior/Fence and Bridge/Fence Wood.png")
alex_walk = load("Character and Portrait/Character/Pre-made/Alex/Walk.png")
portrait_sheet = load("Character and Portrait/Portrait/Premade/1.png")


heart_full = cell(ui_bars, 0, 0)
heart_empty = cell(ui_bars, 2, 0)
hand_icon = cell(ui_hud, 0, 3)
mail_icon = cell(ui_hud, 5, 0)
check_icon = cell(ui_hud, 12, 4)
warning_icon = cell(ui_hud, 3, 0)
weather_sun = cell(ui_weather, 0, 0)
clock_icon = ui_clock
sword_icon = cell(weapons, 2, 0)
bow_icon = cell(weapons, 4, 0)
bag_icon = cell(weapons, 0, 0)
shirt_icon = cell(weapons, 1, 2)
boots_icon = cell(weapons, 3, 2)
ring_icon = cell(weapons, 6, 2)
carrot_icon = crop(carrot_growth, 112, 0, 16, 16)
seed_icon = crop(carrot_growth, 0, 0, 16, 16)
berry_icon = cell(crops, 0, 0)
fish_icon = crop(food, 16, 0, 16, 16)
jar_icon = crop(food, 48, 0, 16, 16)
soil_tile = crop(tilled, 0, 0, 32, 32)
wet_soil_tile = crop(tilled, 0, 64, 32, 32)
grass_patch = crop(grass_tiles, 0, 0, 64, 64)
path_patch = crop(path_tiles, 0, 0, 64, 64)
barn = crop(barn_sheet, 0, 0, 80, 72)
player = crop(alex_walk, 0, 32, 32, 32)
portrait = crop(portrait_sheet, 0, 0, 64, 64)


COL = {
    "bg": (25, 35, 28, 255),
    "ink": (58, 31, 22, 255),
    "ink2": (35, 20, 16, 245),
    "panel": (67, 38, 25, 246),
    "panel_dark": (29, 20, 16, 245),
    "line": (194, 103, 45, 255),
    "line_hi": (255, 190, 91, 255),
    "paper": (238, 211, 176, 255),
    "paper2": (247, 226, 192, 255),
    "text": (255, 235, 198, 255),
    "text_dark": (72, 39, 28, 255),
    "green": (86, 180, 68, 255),
    "green2": (42, 119, 54, 255),
    "red": (227, 49, 54, 255),
    "orange": (232, 130, 45, 255),
    "blue": (74, 156, 215, 255),
}


def paste(base, img, xy, scale=1):
    if scale != 1:
        img = img.resize((img.width * scale, img.height * scale), SCALE)
    base.alpha_composite(img, xy)
    return img.size


def text(draw, xy, value, fnt=FONT_BODY, fill=None, stroke=True, anchor=None):
    fill = fill or COL["text"]
    kwargs = {}
    if anchor:
        kwargs["anchor"] = anchor
    if stroke:
        kwargs["stroke_width"] = 2
        kwargs["stroke_fill"] = (29, 16, 12, 210)
    draw.text(xy, value, font=fnt, fill=fill, **kwargs)


def pixel_panel(draw, xy, fill=None, border=None, shadow=True, inset=True):
    x1, y1, x2, y2 = xy
    fill = fill or COL["panel"]
    border = border or COL["line"]
    if shadow:
        draw.rectangle((x1 + 8, y1 + 8, x2 + 8, y2 + 8), fill=(8, 7, 6, 120))
    draw.rectangle((x1, y1, x2, y2), fill=border)
    draw.rectangle((x1 + 4, y1 + 4, x2 - 4, y2 - 4), fill=fill)
    if inset:
        draw.line((x1 + 7, y1 + 7, x2 - 8, y1 + 7), fill=(255, 202, 112, 110), width=2)
        draw.line((x1 + 7, y1 + 7, x1 + 7, y2 - 8), fill=(255, 202, 112, 80), width=2)
        draw.line((x1 + 7, y2 - 8, x2 - 8, y2 - 8), fill=(20, 12, 10, 110), width=2)
        draw.line((x2 - 8, y1 + 7, x2 - 8, y2 - 8), fill=(20, 12, 10, 110), width=2)


def paper_panel(draw, xy):
    pixel_panel(draw, xy, fill=COL["paper"], border=(136, 75, 45, 255), shadow=True, inset=True)
    x1, y1, x2, y2 = xy
    for cx, cy in [(x1 + 16, y1 + 16), (x2 - 16, y1 + 16), (x1 + 16, y2 - 16), (x2 - 16, y2 - 16)]:
        draw.rectangle((cx - 4, cy - 4, cx + 4, cy + 4), fill=(173, 96, 71, 200))


def slot(draw, xy, selected=False, locked=False):
    fill = (94, 51, 29, 255) if not locked else (58, 37, 28, 255)
    border = COL["line_hi"] if selected else (190, 104, 49, 255)
    x1, y1, x2, y2 = xy
    draw.rectangle((x1 + 4, y1 + 4, x2 + 4, y2 + 4), fill=(8, 7, 6, 100))
    draw.rectangle((x1, y1, x2, y2), fill=border)
    draw.rectangle((x1 + 3, y1 + 3, x2 - 3, y2 - 3), fill=fill)
    draw.line((x1 + 4, y1 + 4, x2 - 4, y1 + 4), fill=(255, 189, 91, 80), width=1)


def progress(draw, xy, pct, color=None, bg=(54, 31, 22, 255)):
    x1, y1, x2, y2 = xy
    color = color or COL["green"]
    draw.rectangle((x1, y1, x2, y2), fill=(219, 128, 52, 255))
    draw.rectangle((x1 + 3, y1 + 3, x2 - 3, y2 - 3), fill=bg)
    fill_w = int((x2 - x1 - 8) * max(0, min(1, pct)))
    draw.rectangle((x1 + 4, y1 + 4, x1 + 4 + fill_w, y2 - 4), fill=color)


def draw_grass_scene(base, draw, xy, size):
    x, y = xy
    w, h = size
    draw.rectangle((x, y, x + w, y + h), fill=(70, 133, 56, 255))
    for yy in range(y, y + h, 64):
        for xx in range(x, x + w, 64):
            if (xx // 64 + yy // 64) % 5 == 0:
                paste(base, grass_patch, (xx, yy), 1)
    for i in range(8):
        px = x + 110 + i * 74
        py = y + 92 + (i % 2) * 12
        draw.rectangle((px, py, px + 12, py + 6), fill=(54, 116, 45, 180))
    paste(base, barn, (x + 58, y + 54), 2)
    for row in range(4):
        for col in range(5):
            sx = x + 350 + col * 58
            sy = y + 260 + row * 46
            paste(base, wet_soil_tile if row == 0 else soil_tile, (sx, sy), 1)
            crop_stage = [16, 48, 80, 112][min(row, 3)]
            paste(base, crop(carrot_growth, crop_stage, 0, 16, 16), (sx + 8, sy + 4), 2)
    for col in range(8):
        paste(base, crop(fence, 0, 0, 32, 32), (x + 86 + col * 40, y + 492), 1)
    paste(base, player, (x + 610, y + 336), 3)
    draw.rectangle((x, y, x + w, y + h), outline=(42, 78, 38, 255), width=6)


def draw_status_cluster(base, draw, x, y, compact=False):
    pixel_panel(draw, (x, y, x + 328, y + 108), fill=(30, 21, 16, 226), border=(204, 112, 49, 255))
    paste(base, portrait, (x + 14, y + 18), 1)
    text(draw, (x + 92, y + 16), "Vida", FONT_BODY)
    for i in range(5):
        paste(base, heart_full if i < 4 else heart_empty, (x + 152 + i * 29, y + 14), 2)
    text(draw, (x + 92, y + 58), "Hambre", FONT_BODY)
    progress(draw, (x + 176, y + 63, x + 306, y + 81), 0.72, COL["green"])
    paste(base, fish_icon, (x + 148, y + 58), 2)


def draw_weapon_selector(base, draw, x, y, selected=0, label=True):
    icons = [(sword_icon, "Espada"), (bow_icon, "Arco")]
    for i, (icon, name) in enumerate(icons):
        sx = x + i * 82
        slot(draw, (sx, y, sx + 62, y + 62), selected=i == selected)
        paste(base, icon, (sx + 15, y + 11), 2)
        if label:
            text(draw, (sx + 31, y + 68), name, FONT_SMALL, anchor="ma")


def draw_context_action(base, draw, x, y):
    pixel_panel(draw, (x, y, x + 238, y + 64), fill=COL["paper2"], border=(129, 76, 46, 255), shadow=True)
    paste(base, hand_icon, (x + 18, y + 18), 2)
    text(draw, (x + 72, y + 18), "Interactuar", FONT_H2, COL["text_dark"], stroke=False)
    pixel_panel(draw, (x + 16, y + 80, x + 310, y + 128), fill=(45, 28, 20, 244), border=(205, 113, 49, 255), shadow=True)
    text(draw, (x + 32, y + 91), "Plantando semilla", FONT_REG)
    progress(draw, (x + 182, y + 94, x + 304, y + 114), 0.58, COL["green"])


def draw_top_clock(base, draw, x, y):
    pixel_panel(draw, (x, y, x + 230, y + 72), fill=(33, 24, 18, 232), border=(204, 112, 49, 255))
    paste(base, clock_icon, (x + 14, y + 20), 1)
    paste(base, weather_sun, (x + 54, y + 20), 2)
    text(draw, (x + 96, y + 12), "Dia 5", FONT_H2)
    text(draw, (x + 96, y + 40), "08:20", FONT_BODY)


def draw_inventory_grid(base, draw, x, y, cols, rows, size=52, items=None):
    if items is None:
        items = [carrot_icon, seed_icon, berry_icon, fish_icon, jar_icon, bag_icon, sword_icon, bow_icon]
    k = 0
    for row in range(rows):
        for col in range(cols):
            sx, sy = x + col * size, y + row * size
            slot(draw, (sx, sy, sx + size - 10, sy + size - 10), selected=(k == 0))
            if k < len(items):
                paste(base, items[k], (sx + 9, sy + 8), 2)
                if k in (0, 2, 3):
                    text(draw, (sx + size - 19, sy + size - 28), str([12, 4, 7][(0, 2, 3).index(k)]), FONT_SMALL, anchor="ma")
            k += 1


def create_gameplay_hud():
    base = Image.new("RGBA", CANVAS, COL["bg"])
    draw = ImageDraw.Draw(base)
    draw_grass_scene(base, draw, (72, 74), (1456, 732))
    draw_status_cluster(base, draw, 104, 104)
    draw_top_clock(base, draw, 686, 104)
    pixel_panel(draw, (1186, 104, 1498, 204), fill=(30, 21, 16, 226), border=(204, 112, 49, 255))
    paste(base, carrot_icon, (1210, 129), 2)
    text(draw, (1252, 124), "Cosecha 3 zanahorias", FONT_BODY)
    progress(draw, (1252, 156, 1470, 174), 0.66)
    text(draw, (1468, 124), "2/3", FONT_SMALL, anchor="ra")
    draw_weapon_selector(base, draw, 702, 712, selected=0)
    draw_context_action(base, draw, 862, 366)
    pixel_panel(draw, (1158, 652, 1498, 770), fill=(30, 21, 16, 212), border=(81, 137, 74, 255))
    text(draw, (1186, 674), "Controles moviles", FONT_BODY)
    paste(base, hand_icon, (1200, 714), 2)
    text(draw, (1242, 716), "accion contextual junto al objeto", FONT_REG)
    text(draw, (88, 824), "HUD principal - limpio, legible y listo para movil/PC", FONT_H1)
    return base


def create_inventory_equipment():
    base = Image.new("RGBA", CANVAS, (46, 28, 21, 255))
    draw = ImageDraw.Draw(base)
    paper_panel(draw, (82, 74, 1518, 806))
    text(draw, (118, 104), "Mochila", FONT_TITLE, COL["text_dark"], stroke=False)
    text(draw, (792, 104), "Equipo", FONT_TITLE, COL["text_dark"], stroke=False)
    for i, tab in enumerate(["Todo", "Comida", "Materiales", "Equipo"]):
        selected = i == 0
        pixel_panel(draw, (118 + i * 150, 156, 246 + i * 150, 200), fill=(178, 98, 43, 255) if selected else (93, 54, 34, 245), border=COL["line_hi"] if selected else (145, 79, 43, 255), shadow=False)
        text(draw, (182 + i * 150, 168), tab, FONT_REG, anchor="ma")
    draw_inventory_grid(base, draw, 122, 236, 8, 5, 58)
    pixel_panel(draw, (122, 560, 606, 738), fill=(64, 38, 26, 245), border=(180, 98, 48, 255))
    paste(base, carrot_icon, (148, 594), 4)
    text(draw, (232, 586), "Zanahoria", FONT_H2)
    text(draw, (232, 622), "Comida basica de la granja.", FONT_REG)
    progress(draw, (232, 666, 530, 686), 0.40, COL["orange"])
    text(draw, (232, 700), "+25 hambre", FONT_BODY)
    draw.line((758, 128, 758, 760), fill=(177, 101, 76, 255), width=4)
    paste(base, portrait, (938, 182), 4)
    text(draw, (1036, 454), "Alex", FONT_H1, COL["text_dark"], stroke=False, anchor="ma")
    equip = [(shirt_icon, "Ropa"), (boots_icon, "Botas"), (ring_icon, "Anillo"), (sword_icon, "Espada"), (bow_icon, "Arco")]
    positions = [(836, 258), (836, 344), (836, 430), (1190, 300), (1190, 386)]
    for i, ((icon, name), pos) in enumerate(zip(equip, positions)):
        sx, sy = pos
        slot(draw, (sx, sy, sx + 68, sy + 68), selected=i == 3)
        paste(base, icon, (sx + 18, sy + 18), 2)
        text(draw, (sx + 34, sy + 76), name, FONT_SMALL, COL["text_dark"], stroke=False, anchor="ma")
    pixel_panel(draw, (844, 560, 1366, 738), fill=(63, 39, 28, 245), border=(180, 98, 48, 255))
    text(draw, (878, 586), "Estado del jugador", FONT_H2)
    for i in range(5):
        paste(base, heart_full if i < 4 else heart_empty, (878 + i * 34, 628), 2)
    text(draw, (878, 678), "Hambre", FONT_BODY)
    progress(draw, (978, 683, 1294, 704), 0.72, COL["green"])
    text(draw, (96, 826), "Inventario / equipamiento - slots claros, lectura rapida, solo espada y arco equipables", FONT_H1)
    return base


def create_quest_journal():
    base = Image.new("RGBA", CANVAS, (34, 25, 20, 255))
    draw = ImageDraw.Draw(base)
    paste(base, crop(ui_book, 0, 0, 246, 230), (72, 58), 5)
    paper_panel(draw, (892, 92, 1508, 786))
    text(draw, (150, 116), "Diario", FONT_TITLE, COL["text_dark"], stroke=False)
    tabs = [("Todas", True), ("Historia", False), ("Granja", False), ("Supervivencia", False)]
    for i, (tab, selected) in enumerate(tabs):
        pixel_panel(draw, (148, 184 + i * 64, 396, 232 + i * 64), fill=(179, 99, 45, 255) if selected else (238, 211, 176, 255), border=(136, 75, 45, 255), shadow=False)
        text(draw, (176, 196 + i * 64), tab, FONT_BODY, COL["text_dark"], stroke=False)
    quests = [("Cosecha 3 zanahorias", carrot_icon, 0.66), ("Repara el refugio", mail_icon, 0.25), ("Encuentra comida", fish_icon, 0.78), ("Prepara semillas", seed_icon, 0.35)]
    for i, (name, icon, pct) in enumerate(quests):
        y = 472 + i * 64
        paste(base, icon, (160, y), 2)
        text(draw, (204, y - 2), name, FONT_REG, COL["text_dark"], stroke=False)
        progress(draw, (204, y + 28, 590, y + 44), pct, COL["green"], bg=(96, 55, 33, 255))
    text(draw, (948, 142), "Mision activa", FONT_TITLE, COL["text_dark"], stroke=False)
    paste(base, carrot_icon, (956, 222), 4)
    text(draw, (1040, 224), "Cosecha 3 zanahorias", FONT_H1, COL["text_dark"], stroke=False)
    text(draw, (1040, 274), "Cultiva, riega y recoge zanahorias para asegurar comida.", FONT_REG, COL["text_dark"], stroke=False)
    progress(draw, (1040, 334, 1438, 360), 0.66, COL["green"], bg=(96, 55, 33, 255))
    text(draw, (1440, 330), "2/3", FONT_BODY, COL["text_dark"], stroke=False, anchor="ra")
    pixel_panel(draw, (958, 420, 1458, 588), fill=(92, 54, 34, 245), border=(187, 105, 50, 255))
    text(draw, (990, 446), "Recompensas", FONT_H2)
    paste(base, carrot_icon, (1002, 500), 2)
    text(draw, (1042, 500), "x3 comida", FONT_BODY)
    paste(base, seed_icon, (1220, 500), 2)
    text(draw, (1260, 500), "x5 semillas", FONT_BODY)
    pixel_panel(draw, (958, 628, 1458, 720), fill=(42, 30, 23, 245), border=(187, 105, 50, 255))
    paste(base, check_icon, (988, 654), 2)
    text(draw, (1032, 654), "Seguimiento visible en HUD", FONT_BODY)
    text(draw, (88, 826), "Misiones - estilo libro del pack, objetivos claros y recompensas visibles", FONT_H1)
    return base


def create_style_variants():
    base = Image.new("RGBA", CANVAS, (24, 32, 27, 255))
    draw = ImageDraw.Draw(base)
    text(draw, (70, 56), "Variantes para elegir direccion visual", FONT_TITLE)
    variants = [
        ("A. Calido / Granja", (70, 130), (76, 43, 28, 246), (215, 116, 47, 255), COL["paper2"]),
        ("B. Survival compacto", (70, 386), (20, 18, 15, 238), (81, 137, 74, 255), (30, 21, 16, 245)),
        ("C. Libro / Aventura", (70, 642), (238, 211, 176, 255), (136, 75, 45, 255), COL["paper"]),
    ]
    for title, pos, fill, border, action_fill in variants:
        x, y = pos
        pixel_panel(draw, (x, y, x + 1460, y + 190), fill=fill, border=border)
        text(draw, (x + 28, y + 22), title, FONT_H1, COL["text_dark"] if fill == COL["paper"] else COL["text"], stroke=fill != COL["paper"])
        draw_status_cluster(base, draw, x + 28, y + 72)
        draw_weapon_selector(base, draw, x + 430, y + 86, selected=0)
        pixel_panel(draw, (x + 660, y + 82, x + 908, y + 146), fill=action_fill, border=border, shadow=False)
        paste(base, hand_icon, (x + 684, y + 100), 2)
        dark_text = action_fill == COL["paper"] or action_fill == COL["paper2"]
        text(draw, (x + 734, y + 100), "Interactuar", FONT_H2, COL["text_dark"] if dark_text else COL["text"], stroke=not dark_text)
        pixel_panel(draw, (x + 980, y + 52, x + 1418, y + 158), fill=(32, 23, 18, 226) if fill != COL["paper"] else (247, 226, 192, 255), border=border, shadow=False)
        paste(base, carrot_icon, (x + 1008, y + 78), 2)
        text(draw, (x + 1052, y + 72), "Cosecha 3 zanahorias", FONT_BODY, COL["text_dark"] if fill == COL["paper"] else COL["text"], stroke=fill != COL["paper"])
        progress(draw, (x + 1052, y + 112, x + 1374, y + 132), 0.66)
    return base


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    images = [
        ("ui_pro_v2_01_gameplay_hud.png", create_gameplay_hud()),
        ("ui_pro_v2_02_inventory_equipment.png", create_inventory_equipment()),
        ("ui_pro_v2_03_quest_journal.png", create_quest_journal()),
        ("ui_pro_v2_04_hud_style_variants.png", create_style_variants()),
    ]
    for name, image in images:
        path = OUT / name
        image.convert("RGB").save(path, quality=95)
        print(path)


if __name__ == "__main__":
    main()
