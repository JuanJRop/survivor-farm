from pathlib import Path
from PIL import Image,ImageDraw
p=Path('Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Character and Portrait/Character/Pre-made/Josh')
names=['Idle.png','Sword.png','Watering.png','Throwing items.png','Pick Up Itens/Pick Up Itens.png','Fishing/Casting.png','Horse/Horse Run.png','Flute/Flute.png','Bicycle/Run Bicycle.png','Bicycle/Run.png']
o=Image.new('RGB',(1100,2400),'#65816a');d=ImageDraw.Draw(o);y=0
for n in names:
 im=Image.open(p/n).convert('RGBA');scale=min(2,1000/im.width)
 im=im.resize((int(im.width*scale),int(im.height*scale)),Image.Resampling.NEAREST)
 d.text((5,y),n,fill='white');o.paste(im,(5,y+16),im);y+=im.height+24
o.crop((0,0,1100,y)).save('Design/Previews/OriginalSprites/player-sheets.jpg')
