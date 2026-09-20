from pathlib import Path
from random import Random

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "Assets" / "SurvivorFarm" / "Art" / "Sprites" / "FarmRPGTinyAssetPack"
OUT = ROOT / "Assets" / "SurvivorFarm" / "Art" / "UIConcepts" / "ProfessionalV3"

CANVAS = (1600, 900)
SCALE = Image.Resampling.NEAREST


def load(rel):
    return Image.open(PACK / rel).convert("RGBA")


def crop(img, x, y, w, h):
    return img.crop((x, y, x + w, y + h))


def cell(img, col, row, size=16):
    return crop(img, col * size, row * size, size, size)


def font(size, bold=False):
    candidates = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/trebucbd.ttf" if bold else "C:/Windows/Fonts/trebuc.ttf",
        "C:/Windows/Fonts/verdana.ttf",
    ]
    for path in candidates:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


FONT_40 = font(40, True)
FONT_34 = font(34, True)
FONT_28 = font(28, True)
FONT_24 = font(24, True)
FONT_20 = font(20, True)
FONT_18 = font(18, True)
FONT_16 = font(16, False)
FONT_14 = font(14, True)


ui_hud = load("UI/HUD.png")
ui_bars = load("UI/Bars.png")
ui_clock = load("UI/Clock/Clock.png")
ui_weather = load("UI/weather icons.png")
ui_book = load("UI/Inventory/Book.png")
ui_slots = load("UI/Inventory/Slots.png")
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
well_sheet = load("Exterior/Well .png")
tree_sheet = load("Farm and Tileset/Tree/Common/Shadow/Maple Tree.png")
stones_sheet = load("Farm and Tileset/Props/Spring/Stones.png")
alex_walk = load("Character and Portrait/Character/Pre-made/Alex/Walk.png")
alex_shovel = load("Character and Portrait/Character/Pre-made/Alex/Shovel.png")
alex_hoe = load("Character and Portrait/Character/Pre-made/Alex/Hoe.png")
alex_water = load("Character and Portrait/Character/Pre-made/Alex/Watering.png")
portrait_sheet = load("Character and Portrait/Portrait/Premade/1.png")


heart_full = cell(ui_bars, 0, 0)
heart_empty = cell(ui_bars, 2, 0)
hand_icon = cell(ui_hud, 0, 3)
quest_icon = cell(ui_hud, 7, 1)
check_icon = cell(ui_hud, 12, 4)
clock_icon = ui_clock
weather_sun = cell(ui_weather, 0, 0)
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
grass_detail = crop(grass_tiles, 0, 0, 32, 32)
path_tile = crop(path_tiles, 0, 0, 32, 32)
barn = crop(barn_sheet, 0, 0, 80, 72)
well = crop(well_sheet, 0, 0, 64, 64)
tree_green = crop(tree_sheet, 0, 28, 62, 84)
tree_orange = crop(tree_sheet, 96, 28, 62, 84)
stone = crop(stones_sheet, 0, 0, 32, 32)
player_idle = crop(alex_walk, 0, 32, 32, 32)
player_shovel = crop(alex_shovel, 0, 64, 32, 32)
player_hoe = crop(alex_hoe, 96, 64, 32, 32)
player_water = crop(alex_water, 96, 0, 32, 32)
portrait = crop(portrait_sheet, 0, 0, 64, 64)
slot_single = crop(ui_slots, 0, 0, 40, 40)
slot_selected = crop(ui_slots, 0, 32, 40, 40)
slot_row = crop(ui_slots, 0, 128, 176, 32)
book_open = crop(ui_book, 0, 0, 256, 208)


COL = {
    "screen": (17, 29, 23, 255),
    "grass": (72, 137, 58, 255),
    "grass_dark": (47, 105, 47, 255),
    "grass_light": (105, 171, 71, 255),
    "ink": (61, 34, 25, 255),
    "ink_soft": (94, 57, 38, 255),
    "wood": (66, 38, 26, 246),
    "wood_dark": (32, 21, 17, 242),
    "paper": (240, 212, 177, 255),
    "paper_light": (250, 229, 193, 255),
    "line": (191, 101, 44, 255),
    "line_hi": (255, 184, 78, 255),
    "green": (88, 178, 68, 255),
    "green_dim": (49, 126, 59, 255),
    "blue": (52, 159, 204, 255),
    "red": (224, 48, 54, 255),
    "cream": (255, 236, 198, 255),
}


def paste(base, img, xy, scale=1):
    if scale != 1:
        img = img.resize((img.width * scale, img.height * scale), SCALE)
    base.alpha_composite(img, xy)
    return img.size


def paste_center(base, img, center, scale=1):
    size = (img.width * scale, img.height * scale)
    xy = (int(center[0] - size[0] / 2), int(center[1] - size[1] / 2))
    paste(base, img, xy, scale)


def label(draw, xy, value, fnt=FONT_18, fill=None, stroke=True, anchor=None):
    fill = fill or COL["cream"]
    kwargs = {}
    if anchor:
        kwargs["anchor"] = anchor
    if stroke:
        kwargs["stroke_width"] = 2
        kwargs["stroke_fill"] = (30, 17, 13, 210)
    draw.text(xy, value, font=fnt, fill=fill, **kwargs)


def box(draw, xy, fill=None, border=None, shadow=True, inset=True):
    x1, y1, x2, y2 = xy
    fill = fill or COL["wood"]
    border = border or COL["line"]
    if shadow:
        draw.rectangle((x1 + 8, y1 + 8, x2 + 8, y2 + 8), fill=(5, 4, 4, 125))
    draw.rectangle((x1, y1, x2, y2), fill=(35, 18, 14, 255))
    draw.rectangle((x1 + 3, y1 + 3, x2 - 3, y2 - 3), fill=border)
    draw.rectangle((x1 + 7, y1 + 7, x2 - 7, y2 - 7), fill=fill)
    if inset:
        draw.line((x1 + 10, y1 + 10, x2 - 11, y1 + 10), fill=(255, 202, 104, 100), width=2)
        draw.line((x1 + 10, y1 + 10, x1 + 10, y2 - 11), fill=(255, 202, 104, 75), width=2)
        draw.line((x1 + 10, y2 - 11, x2 - 11, y2 - 11), fill=(20, 11, 9, 110), width=2)
        draw.line((x2 - 11, y1 + 10, x2 - 11, y2 - 11), fill=(20, 11, 9, 120), width=2)


def paper_box(draw, xy, shadow=True):
    box(draw, xy, fill=COL["paper"], border=(146, 82, 51, 255), shadow=shadow)
    x1, y1, x2, y2 = xy
    for cx, cy in [(x1 + 18, y1 + 18), (x2 - 18, y1 + 18), (x1 + 18, y2 - 18), (x2 - 18, y2 - 18)]:
        draw.rectangle((cx - 4, cy - 4, cx + 4, cy + 4), fill=(175, 96, 74, 220))


def bar(draw, xy, pct, color=None, bg=(57, 34, 25, 255)):
    x1, y1, x2, y2 = xy
    color = color or COL["green"]
    draw.rectangle((x1, y1, x2, y2), fill=COL["line"])
    draw.rectangle((x1 + 3, y1 + 3, x2 - 3, y2 - 3), fill=bg)
    fill_width = int((x2 - x1 - 8) * max(0, min(1, pct)))
    draw.rectangle((x1 + 4, y1 + 4, x1 + 4 + fill_width, y2 - 4), fill=color)
    draw.rectangle((x1 + 4, y1 + 4, x1 + 4 + min(fill_width, 28), y1 + 7), fill=(145, 225, 96, 105))


def slot(draw, xy, selected=False):
    x1, y1, x2, y2 = xy
    sprite = slot_selected if selected else slot_single
    sw, sh = x2 - x1, y2 - y1
    draw.rectangle((x1 + 4, y1 + 5, x2 + 4, y2 + 5), fill=(5, 4, 4, 120))
    paste(slot_layer, sprite, (x1, y1), max(1, sw // sprite.width))


# The slot helper needs a current canvas. It is set at the start of every image.
slot_layer = None


def draw_slot(base, draw, xy, icon=None, selected=False, count=None, scale_icon=2):
    global slot_layer
    slot_layer = base
    x1, y1, x2, y2 = xy
    scale = max(1, (x2 - x1) // slot_single.width)
    sprite = slot_selected if selected else slot_single
    draw.rectangle((x1 + 5, y1 + 6, x1 + sprite.width * scale + 5, y1 + sprite.height * scale + 6), fill=(5, 4, 4, 120))
    paste(base, sprite, (x1, y1), scale)
    if icon is not None:
        paste_center(base, icon, (x1 + sprite.width * scale / 2, y1 + sprite.height * scale / 2), scale_icon)
    if count is not None:
        label(draw, (x1 + sprite.width * scale - 12, y1 + sprite.height * scale - 22), str(count), FONT_14, anchor="ra")


def draw_world(base, draw):
    rnd = Random(13)
    draw.rectangle((58, 52, 1542, 826), fill=COL["grass"])
    for y in range(52, 826, 32):
        for x in range(58, 1542, 32):
            if rnd.random() < 0.25:
                draw.rectangle((x + 12, y + 14, x + 18, y + 18), fill=COL["grass_dark"])
            if rnd.random() < 0.09:
                paste(base, grass_detail, (x, y), 1)

    for i in range(18):
        paste(base, path_tile, (284 + i * 32, 598), 1)
    for i in range(8):
        paste(base, path_tile, (572, 374 + i * 32), 1)

    paste(base, barn, (120, 105), 2)
    paste(base, well, (1280, 230), 2)
    paste(base, tree_green, (1260, 560), 2)
    paste(base, tree_orange, (150, 552), 2)
    paste(base, stone, (372, 722), 1)
    paste(base, stone, (1364, 708), 1)

    for col in range(9):
        paste(base, crop(fence, 0, 0, 32, 32), (144 + col * 39, 505), 1)

    farm_x, farm_y = 454, 330
    for row in range(4):
        for col in range(5):
            x = farm_x + col * 54
            y = farm_y + row * 44
            paste(base, wet_soil_tile if row == 0 else soil_tile, (x, y), 1)
            stage_x = [0, 32, 64, 112][min(row, 3)]
            paste(base, crop(carrot_growth, stage_x, 0, 16, 16), (x + 8, y + 4), 2)

    paste(base, player_idle, (726, 418), 3)
    draw.rectangle((58, 52, 1542, 826), outline=(42, 83, 39, 255), width=6)


def draw_status(base, draw, x, y, variant="wood"):
    fill = COL["wood_dark"] if variant == "wood" else COL["paper_light"]
    text_fill = COL["cream"] if variant == "wood" else COL["ink"]
    box(draw, (x, y, x + 342, y + 118), fill=fill, border=COL["line"], shadow=True)
    paste(base, portrait, (x + 14, y + 18), 1)
    label(draw, (x + 92, y + 19), "Vida", FONT_18, text_fill, stroke=variant == "wood")
    for i in range(5):
        paste(base, heart_full if i < 4 else heart_empty, (x + 152 + i * 29, y + 14), 2)
    label(draw, (x + 92, y + 64), "Hambre", FONT_18, text_fill, stroke=variant == "wood")
    paste(base, fish_icon, (x + 158, y + 61), 2)
    bar(draw, (x + 190, y + 67, x + 316, y + 85), 0.72, COL["green"])


def draw_clock(base, draw, x, y):
    box(draw, (x, y, x + 232, y + 78), fill=(34, 24, 18, 238), border=COL["line"], shadow=True)
    paste(base, clock_icon, (x + 15, y + 23), 1)
    paste(base, weather_sun, (x + 54, y + 23), 2)
    label(draw, (x + 98, y + 13), "Dia 5", FONT_24)
    label(draw, (x + 98, y + 43), "08:20", FONT_18)


def draw_quest_tracker(base, draw, x, y, variant="wood"):
    fill = (32, 23, 18, 238) if variant == "wood" else COL["paper_light"]
    text_fill = COL["cream"] if variant == "wood" else COL["ink"]
    box(draw, (x, y, x + 322, y + 102), fill=fill, border=COL["line"], shadow=True)
    paste(base, carrot_icon, (x + 22, y + 26), 2)
    label(draw, (x + 64, y + 21), "Cosecha zanahorias", FONT_18, text_fill, stroke=variant == "wood")
    label(draw, (x + 282, y + 21), "2/3", FONT_14, text_fill, stroke=variant == "wood", anchor="ra")
    bar(draw, (x + 64, y + 58, x + 292, y + 76), 0.66, COL["green"])


def draw_weapon_bar(base, draw, x, y, compact=False):
    paste(base, slot_row, (x - 26, y - 14), 3)
    draw_slot(base, draw, (x + 16, y, x + 96, y + 80), sword_icon, True, None, 2)
    draw_slot(base, draw, (x + 104, y, x + 184, y + 80), bow_icon, False, None, 2)
    if not compact:
        label(draw, (x + 56, y + 78), "Espada", FONT_14, anchor="ma")
        label(draw, (x + 144, y + 78), "Arco", FONT_14, anchor="ma")


def draw_farm_action(base, draw, x, y):
    paper_box(draw, (x, y, x + 252, y + 70), shadow=True)
    paste(base, hand_icon, (x + 18, y + 20), 2)
    label(draw, (x + 70, y + 22), "Interactuar", FONT_24, COL["ink"], stroke=False)
    box(draw, (x + 26, y + 82, x + 390, y + 272), fill=(34, 23, 18, 238), border=COL["line"], shadow=True)
    steps = [
        ("Cavar", player_shovel, 1.0),
        ("Labrar", player_hoe, 1.0),
        ("Regar", player_water, 0.64),
        ("Sembrar", seed_icon, 0.18),
    ]
    for i, (name, icon, pct) in enumerate(steps):
        row_y = y + 104 + i * 43
        if icon is seed_icon:
            paste(base, icon, (x + 55, row_y + 4), 2)
        else:
            paste(base, icon, (x + 38, row_y - 17), 2)
        label(draw, (x + 112, row_y), name, FONT_16)
        bar(draw, (x + 206, row_y + 3, x + 366, row_y + 19), pct, COL["green"] if pct >= 1 else COL["blue"])


def draw_inventory_items(base, draw, x, y, cols=7, rows=5):
    items = [
        (carrot_icon, 12),
        (seed_icon, 9),
        (berry_icon, 7),
        (fish_icon, 2),
        (jar_icon, 1),
        (bag_icon, None),
        (sword_icon, None),
        (bow_icon, None),
    ]
    idx = 0
    for r in range(rows):
        for c in range(cols):
            sx, sy = x + c * 68, y + r * 62
            icon, count = items[idx] if idx < len(items) else (None, None)
            draw_slot(base, draw, (sx, sy, sx + 64, sy + 64), icon, selected=idx == 0, count=count, scale_icon=2)
            idx += 1


def draw_tabs(draw, x, y, labels, selected=0):
    for i, tab in enumerate(labels):
        fill = (190, 101, 43, 255) if i == selected else (103, 61, 38, 255)
        border = COL["line_hi"] if i == selected else (133, 75, 45, 255)
        box(draw, (x + i * 132, y, x + i * 132 + 116, y + 42), fill=fill, border=border, shadow=False, inset=False)
        label(draw, (x + i * 132 + 58, y + 10), tab, FONT_16, anchor="ma")


def create_ingame_hud():
    base = Image.new("RGBA", CANVAS, COL["screen"])
    draw = ImageDraw.Draw(base)
    draw_world(base, draw)
    draw_status(base, draw, 90, 88)
    draw_clock(base, draw, 684, 86)
    draw_quest_tracker(base, draw, 1188, 92)
    draw_weapon_bar(base, draw, 660, 714)
    draw_farm_action(base, draw, 872, 342)
    return base


def create_inventory_equipment():
    base = Image.new("RGBA", CANVAS, (42, 27, 22, 255))
    draw = ImageDraw.Draw(base)
    paper_box(draw, (80, 74, 1520, 806), shadow=True)
    draw.line((760, 124, 760, 756), fill=(174, 97, 70, 255), width=4)
    label(draw, (118, 106), "Mochila", FONT_40, COL["ink"], stroke=False)
    label(draw, (804, 106), "Equipo", FONT_40, COL["ink"], stroke=False)
    draw_tabs(draw, 118, 160, ["Todo", "Comida", "Material", "Equipo"], 0)
    draw_inventory_items(base, draw, 122, 236)

    box(draw, (122, 594, 654, 740), fill=(70, 42, 28, 245), border=COL["line"], shadow=True)
    paste(base, carrot_icon, (154, 628), 4)
    label(draw, (236, 622), "Zanahoria", FONT_24)
    label(draw, (236, 656), "Comida de granja", FONT_16)
    bar(draw, (236, 690, 548, 710), 0.40, (236, 137, 47, 255))
    label(draw, (566, 684), "+25", FONT_20)

    paste(base, portrait, (960, 198), 4)
    label(draw, (1088, 466), "Alex", FONT_34, COL["ink"], stroke=False, anchor="ma")

    equips = [
        (shirt_icon, "Ropa", (844, 268), False),
        (boots_icon, "Botas", (844, 368), False),
        (ring_icon, "Anillo", (844, 468), False),
        (sword_icon, "Espada", (1210, 322), True),
        (bow_icon, "Arco", (1210, 422), False),
    ]
    for icon, name, pos, selected in equips:
        draw_slot(base, draw, (pos[0], pos[1], pos[0] + 80, pos[1] + 80), icon, selected=selected, scale_icon=2)
        label(draw, (pos[0] + 40, pos[1] + 88), name, FONT_14, COL["ink"], stroke=False, anchor="ma")

    box(draw, (844, 590, 1378, 740), fill=(57, 37, 27, 245), border=COL["line"], shadow=True)
    label(draw, (878, 614), "Estado", FONT_24)
    for i in range(5):
        paste(base, heart_full if i < 4 else heart_empty, (878 + i * 34, 654), 2)
    paste(base, fish_icon, (878, 700), 2)
    bar(draw, (918, 704, 1298, 724), 0.72, COL["green"])
    return base


def create_quest_journal():
    base = Image.new("RGBA", CANVAS, (33, 24, 20, 255))
    draw = ImageDraw.Draw(base)
    paste(base, book_open, (80, 42), 4)
    label(draw, (154, 112), "Diario", FONT_34, COL["ink"], stroke=False)
    draw_tabs(draw, 152, 178, ["Todas", "Granja", "Historia"], 0)

    quest_rows = [
        ("Cosecha zanahorias", carrot_icon, 0.66),
        ("Repara el refugio", quest_icon, 0.25),
        ("Encuentra comida", fish_icon, 0.78),
        ("Prepara semillas", seed_icon, 0.35),
    ]
    for i, (name, icon, pct) in enumerate(quest_rows):
        y = 268 + i * 72
        paste(base, icon, (162, y + 2), 2)
        label(draw, (204, y), name, FONT_18, COL["ink"], stroke=False)
        bar(draw, (204, y + 38, 552, y + 56), pct, COL["green"], bg=(105, 62, 38, 255))

    paper_box(draw, (872, 94, 1508, 792), shadow=True)
    label(draw, (928, 146), "Mision activa", FONT_40, COL["ink"], stroke=False)
    paste(base, carrot_icon, (942, 238), 4)
    label(draw, (1030, 242), "Cosecha zanahorias", FONT_28, COL["ink"], stroke=False)
    label(draw, (1030, 288), "Cultiva, riega y recoge para mantener comida.", FONT_16, COL["ink"], stroke=False)
    bar(draw, (1030, 346, 1438, 372), 0.66, COL["green"], bg=(105, 62, 38, 255))
    label(draw, (1440, 340), "2/3", FONT_18, COL["ink"], stroke=False, anchor="ra")

    box(draw, (948, 438, 1452, 586), fill=(91, 55, 35, 245), border=COL["line"], shadow=True)
    label(draw, (982, 464), "Recompensas", FONT_24)
    paste(base, carrot_icon, (992, 518), 2)
    label(draw, (1032, 518), "x3 comida", FONT_18)
    paste(base, seed_icon, (1236, 518), 2)
    label(draw, (1276, 518), "x5 semillas", FONT_18)

    box(draw, (948, 628, 1452, 720), fill=(39, 30, 24, 245), border=(84, 145, 75, 255), shadow=True)
    paste(base, check_icon, (984, 656), 2)
    label(draw, (1032, 656), "Visible en el HUD", FONT_20)
    return base


def create_hud_variants():
    base = Image.new("RGBA", CANVAS, COL["screen"])
    draw = ImageDraw.Draw(base)
    label(draw, (72, 58), "Tres direcciones visuales", FONT_40)
    variants = [
        ("A. Granja calida", 132, COL["wood"], COL["line"], "wood"),
        ("B. Survival oscuro", 388, (20, 18, 16, 244), (84, 145, 75, 255), "wood"),
        ("C. Libro claro", 644, COL["paper"], (146, 82, 51, 255), "paper"),
    ]
    for title, y, fill, border, kind in variants:
        box(draw, (72, y, 1528, y + 190), fill=fill, border=border, shadow=True)
        label(draw, (102, y + 24), title, FONT_28, COL["ink"] if kind == "paper" else COL["cream"], stroke=kind != "paper")
        draw_status(base, draw, 102, y + 72, variant=kind)
        draw_weapon_bar(base, draw, 504, y + 92, compact=True)
        action_fill = COL["paper_light"] if kind != "wood" else (34, 24, 19, 238)
        box(draw, (738, y + 82, 984, y + 150), fill=action_fill, border=border, shadow=False)
        paste(base, hand_icon, (762, y + 103), 2)
        label(draw, (812, y + 104), "Interactuar", FONT_20, COL["ink"] if kind == "paper" else COL["cream"], stroke=kind != "paper")
        draw_quest_tracker(base, draw, 1082, y + 54, variant=kind)
    return base


def create_mobile_layout():
    base = Image.new("RGBA", CANVAS, COL["screen"])
    draw = ImageDraw.Draw(base)
    draw_world(base, draw)
    draw_status(base, draw, 88, 82)
    draw_quest_tracker(base, draw, 1126, 82)
    draw_clock(base, draw, 684, 82)

    # Mobile touch zones: visual only, based on the existing button and HUD icon sheets.
    for pos, icon in [((115, 648), sword_icon), ((214, 648), bow_icon), ((1328, 648), hand_icon)]:
        selected = icon == sword_icon
        draw_slot(base, draw, (pos[0], pos[1], pos[0] + 88, pos[1] + 88), icon, selected=selected, scale_icon=2)
    box(draw, (1254, 558, 1472, 622), fill=COL["paper_light"], border=COL["line"], shadow=True)
    paste(base, hand_icon, (1280, 578), 2)
    label(draw, (1332, 579), "Interactuar", FONT_20, COL["ink"], stroke=False)
    bar(draw, (1254, 632, 1472, 654), 0.58, COL["blue"])
    return base


def save(name, img):
    path = OUT / name
    img.convert("RGB").save(path, quality=95)
    print(path)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    save("ui_pro_v3_01_ingame_hud.png", create_ingame_hud())
    save("ui_pro_v3_02_inventory_equipment.png", create_inventory_equipment())
    save("ui_pro_v3_03_quest_journal.png", create_quest_journal())
    save("ui_pro_v3_04_hud_variants.png", create_hud_variants())
    save("ui_pro_v3_05_mobile_touch_layout.png", create_mobile_layout())


if __name__ == "__main__":
    main()
