from pathlib import Path
from PIL import Image,ImageDraw
root=Path('Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack')
paths=['Farm and Tileset/Props/Spring/Ground stones.png','Farm and Tileset/Tileset/Tileset House.png','Farm and Tileset/Tileset/Tilled Soil and wet soil.png','Farm and Tileset/Tileset/Caves.png','Exterior/Houses/NPCS houses/Blacksmith.png','Interior/Tables and desks.png']
out=Image.new('RGB',(1200,((len(paths)+1)//2)*430),'#79877e'); d=ImageDraw.Draw(out)
for i,p in enumerate(paths):
 im=Image.open(root/p).convert('RGBA'); print(p,im.size)
 x=(i%2)*600;y=(i//2)*430
 d.text((x+5,y+5),p,fill='white');d.text((x+5,y+20),str(im.size),fill='white')
 scale=min(560/im.width,385/im.height,3)
 im=im.resize((int(im.width*scale),int(im.height*scale)),Image.Resampling.NEAREST)
 out.paste(im,(x+5,y+40),im)
out.save('Design/Previews/OriginalSprites/world-sheets.jpg')
