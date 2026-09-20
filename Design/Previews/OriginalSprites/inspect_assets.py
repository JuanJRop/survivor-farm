from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
root=Path(__file__).resolve().parents[3]/'Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack'
paths=[
'Farm and Tileset/Tileset/Tileset Grass Spring.png',
'Farm and Tileset/Tileset/Path tiles.png',
'Exterior/Houses/Tiny House.png',
'Farm and Tileset/Tree/Common/No Shadow/Maple Tree.png',
'UI/HUD.png','UI/button.png','UI/Bars.png','UI/Inventory/Slots.png','UI/Inventory/inventory.png',
'Icons/Weapons/RPG/1.png','UI/Money.png','Farm Crops/Spring/Carrot.png',
'Exterior/Fence.png','Exterior/shipping box.png','Exterior/Well .png',
'Farm Animals/Chicken/Chicken White.png','Character and Portrait/Character/Pre-made/Josh/Idle.png',
'Farm and Tileset/Tileset/Tilled Soil and wet soil.png']
out=Image.new('RGB',(1200,((len(paths)+2)//3)*280),'#34414b'); d=ImageDraw.Draw(out)
for i,p in enumerate(paths):
 x=(i%3)*400; y=(i//3)*280
 if not (root/p).exists():
  d.text((x+5,y+5),f'{i}: MISSING {p}',fill='white'); continue
 im=Image.open(root/p).convert('RGBA'); print(i,p,im.size)
 d.text((x+5,y+4),f'{i}: {Path(p).name} {im.size}',fill='white')
 s=min(3,390/im.width,248/im.height)
 im=im.resize((int(im.width*s),int(im.height*s)),Image.Resampling.NEAREST)
 out.paste(im,(x+5,y+25),im)
out.save(Path(__file__).with_name('contact.png'))
