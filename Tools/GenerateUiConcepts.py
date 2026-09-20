from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "Assets" / "SurvivorFarm" / "Art" / "Sprites" / "FarmRPGTinyAssetPack"
OUT = ROOT / "Assets" / "SurvivorFarm" / "Art" / "UIConcepts"


def load(path):
    return Image.open(path).convert("RGBA")


def transparent_black(img):
    img = img.copy()
    pixels = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = pixels[x, y]
            if r < 8 and g < 8 and b < 8:
                pixels[x, y] = (0, 0, 0, 0)
    return img


def font(size):
    candidates = [
        "C:/Windows/Fonts/arialbd.ttf",
        "C:/Windows/Fonts/arial.ttf",
    ]
    for candidate in candidates:
        if Path(candidate).exists():
            return ImageFont.truetype(candidate, size)
    return ImageFont.load_default()


F_TITLE = font(30)
F_H2 = font(21)
F_BODY = font(16)
F_SMALL = font(13)


def panel(draw, xy, fill=(44, 24, 18, 230), outline=(214, 124, 53, 255), width=3):
    if len(fill) == 4 and fill[3] == 0:
        draw.rounded_rectangle(xy, radius=8, outline=outline, width=width)
    else:
        draw.rounded_rectangle(xy, radius=8, fill=fill, outline=outline, width=width)


def label(draw, xy, text, fill=(255, 238, 194, 255), fnt=F_BODY):
    draw.text(xy, text, font=fnt, fill=fill, stroke_width=2, stroke_fill=(40, 20, 14, 220))


def paste_scaled(base, img, xy, scale=2):
    resized = img.resize((img.width * scale, img.height * scale), Image.Resampling.NEAREST)
    base.alpha_composite(resized, xy)
    return resized.size


def crop_grid(img, col, row, cell=16):
    return img.crop((col * cell, row * cell, col * cell + cell, row * cell + cell))


def crop_rect(img, xywh):
    x, y, w, h = xywh
    return img.crop((x, y, x + w, y + h))


ui_hud = transparent_black(load(PACK / "UI" / "HUD.png"))
ui_bars = transparent_black(load(PACK / "UI" / "Bars.png"))
ui_buttons = transparent_black(load(PACK / "UI" / "button.png"))
ui_slots = transparent_black(load(PACK / "UI" / "Inventory" / "Slots.png"))
ui_book = transparent_black(load(PACK / "UI" / "Inventory" / "Book.png"))
ui_inventory = transparent_black(load(PACK / "UI" / "Inventory" / "inventory.png"))
crop_icons = transparent_black(load(PACK / "Icons" / "Crops.png"))
weapon_icons = transparent_black(load(PACK / "Icons" / "Weapons" / "RPG" / "1.png"))
food_icons = transparent_black(load(PACK / "Icons" / "Food 0.1.png"))
animal_icons = transparent_black(load(PACK / "Icons" / "Farm Animals" / "Animals farm icons.png"))
portrait = transparent_black(load(PACK / "Character and Portrait" / "Portrait" / "Premade" / "1.png"))
tiles = transparent_black(load(PACK / "Farm and Tileset" / "Tileset" / "Tileset Grass Spring.png"))
tree = transparent_black(load(PACK / "Farm and Tileset" / "Tree" / "Common" / "Shadow" / "Maple Tree.png"))


heart_full = crop_grid(ui_bars, 0, 0, 16)
heart_empty = crop_grid(ui_bars, 2, 0, 16)
energy_bar = crop_rect(ui_bars, (0, 72, 80, 8))
hand_icon = crop_grid(ui_hud, 0, 3, 16)
quest_icon = crop_grid(ui_hud, 4, 0, 16)
sword_icon = crop_rect(weapon_icons, (0, 0, 16, 16))
bow_icon = crop_rect(weapon_icons, (16, 0, 16, 16))
carrot_icon = crop_rect(crop_icons, (32, 0, 16, 16))
seed_icon = crop_rect(crop_icons, (0, 0, 16, 16))
apple_icon = crop_rect(food_icons, (0, 0, 16, 16))
cow_icon = crop_rect(animal_icons, (0, 0, 16, 16))
slot_piece = crop_rect(ui_slots, (0, 0, 40, 40))
button_piece = crop_rect(ui_buttons, (0, 0, 64, 16))
book_piece = crop_rect(ui_book, (0, 0, 224, 128))
portrait_face = crop_rect(portrait, (0, 0, 32, 32))


def draw_game_field(base, draw, xy, size):
    x0, y0 = xy
    w, h = size
    draw.rectangle((x0, y0, x0 + w, y0 + h), fill=(70, 130, 57, 255))
    for y in range(y0 + 18, y0 + h - 18, 32):
        for x in range(x0 + 18, x0 + w - 18, 32):
            if (x + y) % 96 == 0:
                draw.rectangle((x, y, x + 8, y + 4), fill=(103, 169, 72, 190))
            elif (x + y) % 128 == 0:
                draw.rectangle((x + 2, y, x + 5, y + 9), fill=(43, 103, 49, 180))
    tile = crop_rect(tiles, (0, 0, 64, 64)).resize((96, 96), Image.Resampling.NEAREST)
    for y in range(y0, y0 + h, tile.height * 2):
        base.alpha_composite(tile, (x0 + w - 96, y))
    for x in range(x0, x0 + w, 96):
        base.alpha_composite(tile, (x, y0 + h - 96))
    panel(draw, (x0, y0, x0 + w, y0 + h), fill=(0, 0, 0, 0), outline=(114, 70, 35, 255), width=5)
    for i in range(4):
        for j in range(3):
            px = x0 + 245 + i * 74
            py = y0 + 255 + j * 52
            draw.rounded_rectangle((px, py, px + 56, py + 38), radius=4, fill=(78, 45, 23, 255), outline=(139, 86, 42, 255), width=2)
            paste_scaled(base, seed_icon if j < 2 else carrot_icon, (px + 19, py + 7), 2)
    paste_scaled(base, tree, (x0 + 80, y0 + 130), 2)
    paste_scaled(base, tree, (x0 + 630, y0 + 120), 2)
    paste_scaled(base, portrait_face, (x0 + 405, y0 + 330), 3)
    paste_scaled(base, cow_icon, (x0 + 160, y0 + 250), 3)


def draw_status(draw, base, x, y, compact=False):
    label(draw, (x, y), "Vida", fnt=F_BODY if compact else F_H2)
    for i in range(5):
        paste_scaled(base, heart_full if i < 4 else heart_empty, (x + 62 + i * 28, y - 2), 2)
    label(draw, (x, y + 34), "Hambre", fnt=F_BODY if compact else F_H2)
    draw.rounded_rectangle((x + 88, y + 39, x + 244, y + 55), radius=4, fill=(24, 15, 11, 240), outline=(214, 124, 53, 255), width=2)
    draw.rounded_rectangle((x + 92, y + 43, x + 198, y + 51), radius=3, fill=(77, 190, 70, 255))


def draw_weapon_bar(draw, base, x, y, selected=0):
    for i, icon in enumerate([sword_icon, bow_icon]):
        sx = x + i * 70
        fill = (92, 54, 30, 235) if i != selected else (142, 82, 36, 250)
        panel(draw, (sx, y, sx + 56, y + 56), fill=fill, outline=(255, 192, 70, 255) if i == selected else (172, 96, 44, 255), width=3)
        paste_scaled(base, icon, (sx + 12, y + 9), 2)
        label(draw, (sx - 2, y + 60), "Espada" if i == 0 else "Arco", fnt=F_SMALL)


def draw_inventory_grid(draw, base, x, y, cols=5, rows=4):
    icons = [carrot_icon, apple_icon, seed_icon, sword_icon, bow_icon, crop_rect(crop_icons, (64, 0, 16, 16)), crop_rect(crop_icons, (96, 0, 16, 16))]
    k = 0
    for row in range(rows):
        for col in range(cols):
            sx = x + col * 50
            sy = y + row * 50
            panel(draw, (sx, sy, sx + 42, sy + 42), fill=(76, 43, 25, 230), outline=(181, 106, 53, 255), width=2)
            if k < len(icons):
                paste_scaled(base, icons[k], (sx + 5, sy + 5), 2)
            k += 1


def concept_cozy():
    base = Image.new("RGBA", (1600, 900), (38, 70, 45, 255))
    draw = ImageDraw.Draw(base)
    draw_game_field(base, draw, (40, 70), (930, 610))
    panel(draw, (40, 20, 610, 64), fill=(76, 43, 25, 250))
    label(draw, (62, 22), "Version A - HUD claro con UI del pack", fnt=F_TITLE)
    panel(draw, (64, 94, 344, 176), fill=(24, 18, 14, 205))
    paste_scaled(base, portrait_face, (78, 105), 2)
    draw_status(draw, base, 148, 104)
    draw_weapon_bar(draw, base, 402, 596, 0)
    panel(draw, (560, 378, 760, 432), fill=(250, 229, 191, 245), outline=(96, 58, 32, 255), width=3)
    paste_scaled(base, hand_icon, (576, 390), 2)
    draw.text((622, 392), "Interactuar", font=F_H2, fill=(40, 23, 16, 255))
    panel(draw, (1010, 70, 1558, 388), fill=(38, 24, 18, 245))
    label(draw, (1032, 90), "Inventario / Equipo", fnt=F_TITLE)
    draw_inventory_grid(draw, base, 1034, 138)
    paste_scaled(base, portrait_face, (1328, 145), 4)
    draw_weapon_bar(draw, base, 1308, 284, 1)
    panel(draw, (1010, 420, 1558, 680), fill=(50, 30, 22, 245))
    label(draw, (1034, 442), "Misiones", fnt=F_TITLE)
    paste_scaled(base, quest_icon, (1038, 500), 2)
    label(draw, (1084, 500), "Cosecha 3 zanahorias", fnt=F_H2)
    draw.rounded_rectangle((1084, 540, 1450, 562), radius=4, fill=(31, 20, 14, 255), outline=(187, 105, 48, 255), width=2)
    draw.rounded_rectangle((1090, 546, 1268, 556), radius=3, fill=(78, 184, 68, 255))
    label(draw, (1084, 580), "Recompensa: comida + semillas", fnt=F_BODY)
    panel(draw, (70, 710, 1530, 852), fill=(21, 24, 22, 230))
    label(draw, (96, 730), "Componentes: corazones, hambre, armas, tooltip, progreso de accion", fnt=F_H2)
    paste_scaled(base, carrot_icon, (116, 780), 3)
    label(draw, (176, 786), "Zanahoria - restaura hambre", fnt=F_BODY)
    draw.rounded_rectangle((620, 786, 940, 812), radius=5, fill=(42, 25, 16, 255), outline=(214, 124, 53, 255), width=2)
    draw.rounded_rectangle((626, 792, 780, 806), radius=4, fill=(78, 184, 68, 255))
    label(draw, (446, 784), "Plantando semilla", fnt=F_BODY)
    return base


def concept_compact():
    base = Image.new("RGBA", (1600, 900), (22, 29, 24, 255))
    draw = ImageDraw.Draw(base)
    draw_game_field(base, draw, (50, 60), (1020, 720))
    draw.rectangle((50, 60, 1070, 780), outline=(59, 89, 60, 255), width=6)
    panel(draw, (76, 84, 372, 166), fill=(8, 10, 8, 210), outline=(90, 142, 73, 255), width=3)
    draw_status(draw, base, 96, 98, compact=True)
    panel(draw, (430, 88, 616, 142), fill=(8, 10, 8, 205), outline=(90, 142, 73, 255), width=2)
    label(draw, (450, 102), "Dia 5  08:20", fnt=F_H2)
    draw_weapon_bar(draw, base, 438, 678, 1)
    panel(draw, (628, 330, 832, 382), fill=(238, 220, 186, 245), outline=(60, 40, 24, 255), width=3)
    paste_scaled(base, hand_icon, (646, 342), 2)
    draw.text((692, 344), "Interactuar", font=F_H2, fill=(38, 22, 14, 255))
    panel(draw, (1120, 60, 1548, 354), fill=(20, 16, 13, 245), outline=(90, 142, 73, 255), width=3)
    label(draw, (1140, 84), "Mochila rapida", fnt=F_TITLE)
    draw_inventory_grid(draw, base, 1144, 140, 6, 3)
    panel(draw, (1120, 386, 1548, 642), fill=(20, 16, 13, 245), outline=(90, 142, 73, 255), width=3)
    label(draw, (1140, 408), "Equipo", fnt=F_TITLE)
    paste_scaled(base, portrait_face, (1192, 468), 4)
    draw_weapon_bar(draw, base, 1360, 478, 0)
    panel(draw, (1120, 674, 1548, 820), fill=(20, 16, 13, 245), outline=(90, 142, 73, 255), width=3)
    label(draw, (1140, 694), "Misiones compactas", fnt=F_H2)
    paste_scaled(base, quest_icon, (1148, 738), 2)
    label(draw, (1192, 742), "Sobrevive 1 noche", fnt=F_BODY)
    label(draw, (50, 818), "Version B - survival compacto, menos pantalla tapada", fnt=F_TITLE)
    return base


def concept_book():
    base = Image.new("RGBA", (1600, 900), (57, 37, 25, 255))
    draw = ImageDraw.Draw(base)
    panel(draw, (36, 28, 1564, 856), fill=(36, 22, 16, 245), outline=(222, 150, 67, 255), width=4)
    label(draw, (66, 48), "Version C - menu libro: inventario, equipo y misiones", fnt=F_TITLE)
    paste_scaled(base, book_piece, (82, 110), 4)
    label(draw, (170, 140), "Inventario", fill=(70, 36, 22, 255), fnt=F_TITLE)
    label(draw, (612, 140), "Equipo", fill=(70, 36, 22, 255), fnt=F_TITLE)
    draw_inventory_grid(draw, base, 168, 214, 6, 4)
    paste_scaled(base, portrait_face, (710, 240), 5)
    draw_weapon_bar(draw, base, 650, 492, 0)
    panel(draw, (1018, 110, 1498, 752), fill=(236, 208, 169, 250), outline=(92, 48, 30, 255), width=4)
    draw.text((1060, 148), "Misiones", font=F_TITLE, fill=(70, 36, 22, 255))
    for i, (txt, pct, icon) in enumerate([
        ("Cosecha 3 zanahorias", 0.45, carrot_icon),
        ("Repara el refugio", 0.25, crop_rect(ui_hud, (96, 0, 16, 16))),
        ("Encuentra comida", 0.75, apple_icon),
    ]):
        y = 220 + i * 128
        paste_scaled(base, icon, (1064, y), 2)
        draw.text((1110, y), txt, font=F_H2, fill=(70, 36, 22, 255))
        draw.rounded_rectangle((1110, y + 42, 1436, y + 64), radius=4, fill=(85, 52, 32, 255), outline=(151, 88, 42, 255), width=2)
        draw.rounded_rectangle((1116, y + 48, int(1116 + 314 * pct), y + 58), radius=3, fill=(77, 180, 68, 255))
    panel(draw, (106, 704, 896, 812), fill=(54, 31, 20, 240))
    label(draw, (132, 724), "HUD en partida: corazones + hambre + espada/arco + boton junto al objeto", fnt=F_H2)
    draw_status(draw, base, 132, 762, compact=True)
    draw_weapon_bar(draw, base, 496, 746, 1)
    return base


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    outputs = [
        ("ui_concept_a_cozy_hud.png", concept_cozy()),
        ("ui_concept_b_compact_survival.png", concept_compact()),
        ("ui_concept_c_book_menus.png", concept_book()),
    ]
    for name, image in outputs:
        image.convert("RGB").save(OUT / name, quality=95)
        print(OUT / name)


if __name__ == "__main__":
    main()
