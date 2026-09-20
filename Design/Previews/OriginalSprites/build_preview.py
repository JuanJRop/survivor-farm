"""Compose a gameplay mockup exclusively from existing project sprites. No generated artwork."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import random, json
OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[2]
ROOT=PROJECT/'Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack'
N=Image.Resampling.NEAREST
used={}
def sprite(path,box=None):
    im=Image.open(ROOT/path).convert('RGBA')
    used.setdefault(path,[])
    if box:
        box=tuple(box)
        assert 0<=box[0]<box[2]<=im.width and 0<=box[1]<box[3]<=im.height,(path,box,im.size)
        if list(box) not in used[path]: used[path].append(list(box))
        im=im.crop(box)
    return im
def paste(im,x,y,scale=1):
    if scale!=1: im=im.resize((im.width*scale,im.height*scale),N)
    screen.alpha_composite(im,(int(x),int(y)))
def nine(im,x,y,w,h,b=4):
    iw,ih=im.size
    sx=[0,b,iw-b,iw]; sy=[0,b,ih-b,ih]
    dx=[x,x+b,x+w-b,x+w]; dy=[y,y+b,y+h-b,y+h]
    for j in range(3):
        for i in range(3):
            piece=im.crop((sx[i],sy[j],sx[i+1],sy[j+1]))
            piece=piece.resize((dx[i+1]-dx[i],dy[j+1]-dy[j]),N)
            paste(piece,dx[i],dy[j])
def panel(x,y,w,h): nine(parchment,x,y,w,h,6)
fontpath=PROJECT/'SurvivorFarm/Assets/Project/UnityDefault/TextMesh Pro/Fonts/LiberationSans.ttf'
def text(s,x,y,size=16,color='#472a29',center=False):
    f=ImageFont.truetype(str(fontpath),size)
    d=ImageDraw.Draw(screen)
    if center: x-=d.textlength(s,font=f)/2
    d.text((int(x),int(y)),s,font=f,fill=color,stroke_width=0)

screen=Image.new('RGBA',(480,854))
grass='Farm and Tileset/Tileset/Tileset Grass Spring.png'
ground=sprite(grass,(88,24,104,40))
for y in range(0,854,32):
    for x in range(0,480,32): paste(ground,x,y,2)
# Repeated original terrain tiles. No painted grass or synthetic texture.
tuft=sprite(grass,(144,64,160,80))
random.seed(20)
for i in range(160): paste(tuft,random.randrange(480),random.randrange(120,854),2)
# Dirt path tiles: top, middle and bottom from the supplied spring terrain atlas.
path=sprite(grass,(88,152,104,168))
edgeL=sprite(grass,(64,152,80,168)); edgeR=sprite(grass,(112,152,128,168))
for y in range(312,854,32):
    paste(edgeL,208,y,2); paste(path,240,y,2); paste(edgeR,272,y,2)
for x in range(0,480,32):
    paste(sprite(grass,(88,128,104,144)),x,352,2)
    paste(path,x,384,2)
    paste(sprite(grass,(88,176,104,192)),x,416,2)
for x in [208,240,272]:
    for y in [352,384,416]: paste(path,x,y,2)
# Existing full premade house, clipped out of its assembly sheet.
house=sprite('Exterior/Houses/Tiny House.png',(80,368,160,480))
paste(house,144,136,2)
tree=sprite('Farm and Tileset/Tree/Common/No Shadow/Maple Tree.png',(0,48,32,96))
for x,y in [(-16,112),(42,148),(-22,244),(400,130),(446,222),(-18,520),(36,548),(422,520),(452,590),(114,590)]: paste(tree,x,y,2)
well=sprite('Exterior/Well .png',(0,0,32,48)); paste(well,340,278,2)
chest=sprite('Exterior/shipping box.png',(0,0,16,32)); paste(chest,118,310,2)
# Soil patches use the source patch as nine-slice, preserving its original boundary.
soil=sprite('Farm and Tileset/Tileset/Tilled Soil and wet soil.png',(16,0,64,48)).resize((96,96),N)
nine(soil,28,468,160,128,32); nine(soil,324,468,128,128,32)
carrot='Farm Crops/Spring/Carrot.png'
for row in range(3):
    for col in range(4): paste(sprite(carrot,(80,0,96,16)),44+col*32,480+row*32,2)
for row in range(3):
    for col in range(3):
        stage=[32,48,64][row]
        paste(sprite(carrot,(stage,0,stage+16,16)),340+col*32,480+row*32,2)
# Chicken frames and playable Josh are copied from the existing animation atlases.
chicken=sprite('Farm Animals/Chicken/Chicken White.png',(0,0,16,16))
paste(chicken,88,405,2); paste(chicken,330,630,2)
hero=sprite('Character and Portrait/Character/Pre-made/Josh/Idle.png',(32,0,64,32))
paste(hero,216,422,2)

# UI art: the pack's parchment, framed slot, status icons and tool icons.
parchment=sprite('UI/Inventory/inventory.png',(0,64,48,112))
slot=sprite('UI/Inventory/Slots.png',(8,8,44,36)).resize((72,56),N)
hud='UI/HUD.png'; bars='UI/Bars.png'; weapons='Icons/Weapons/RPG/1.png'
panel(12,12,268,78); panel(330,12,138,78)
for i in range(5): paste(sprite(bars,(0,0,16,16)),22+i*34,20,2)
text('Hambre 100%',24,61,14)
paste(sprite(bars,(0,96,48,112)),159,50,2)
text('DIA 1',348,25,16)
paste(sprite('UI/Money.png',(0,0,16,16)),342,47,2)
text('240',380,57,18)
panel(12,102,212,44)
paste(sprite(hud,(112,32,128,48)),22,111,2)
text('PRIMERA COSECHA',59,111,13)
text('Recoge 3 zanahorias',59,127,12)

panel(16,650,172,42)
paste(sprite(carrot,(112,0,128,16)),24,655,2)
text('ZANAHORIA',62,657,13)
text('Lista para recoger',62,674,11)
panel(386,592,76,68)
paste(sprite(weapons,(32,0,48,16)),408,598,2)
text('Atacar',424,639,13,center=True)
panel(374,674,88,70)
paste(sprite(hud,(0,48,16,64)),402,680,2)
text('Usar',418,725,14,center=True)
panel(16,704,64,52)
paste(sprite(hud,(48,32,64,48)),32,707,2)
text('Mochila',48,738,11,center=True)
panel(181,736,118,25); text('AZADA',240,743,14,center=True)
panel(12,768,456,74)
toolcols=[32,64,48,16,96,112,0]
for i,col in enumerate(toolcols):
    x=22+i*63
    if i==4: panel(x,778,58,54)
    else: nine(slot,x,778,58,54,10)
    paste(sprite(weapons,(col,0,col+16,16)),x+13,786,2)
    text(str(i+1),x+5,817,10,'#65403b' if i==4 else '#ffdab2')

screen.convert('RGB').resize((960,1708),N).save(OUT/'gameplay-sprites-originales.png')
screen.convert('RGB').save(OUT/'gameplay-base-480x854.png')
(OUT/'sprites-utilizados.json').write_text(json.dumps({'sourceRoot':str(ROOT),'method':'crop, nearest-neighbor scaling, compositing, nine-slice and text; no AI artwork','font':str(fontpath),'sprites':used},ensure_ascii=False,indent=2),encoding='utf8')
print('Saved gameplay-sprites-originales.png with',len(used),'original sprite sheets')
