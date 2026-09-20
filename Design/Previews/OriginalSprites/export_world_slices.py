"""Exact crops from the project's original pack. No generated or repainted art."""
from pathlib import Path
from PIL import Image
import json
root=Path('Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack')
out=Path('Assets/SurvivorFarm/Art/WorldSprites');out.mkdir(parents=True,exist_ok=True)
entries=[]
def crop(name,path,box,ppu=32,pivot=.5):
 im=Image.open(root/path).convert('RGBA');assert box[2]<=im.width and box[3]<=im.height,(name,im.size,box)
 im.crop(box).save(out/(name+'.png'))
 entries.append(dict(name=name,source=path,box=list(box),ppu=ppu,pivot=pivot))
grass='Farm and Tileset/Tileset/Tileset Grass Spring.png'
for n,b in dict(Grass=(88,24,104,40),Tuft=(144,64,160,80),Path=(88,152,104,168),PathLeft=(64,152,80,168),PathRight=(112,152,128,168),PathTop=(88,128,104,144),PathBottom=(88,176,104,192)).items():crop(n,grass,b)
crop('House','Exterior/Houses/Tiny House.png',(80,368,160,480),32,.12)
crop('Shop','Exterior/Houses/NPCS houses/Blacksmith.png',(80,0,224,96),32,.12)
crop('Tree','Farm and Tileset/Tree/Common/No Shadow/Maple Tree.png',(0,48,32,96),24,.12)
crop('Rock','Farm and Tileset/Props/Spring/Ground stones.png',(32,0,48,16),16,.3)
crop('Soil','Farm and Tileset/Tileset/Tilled Soil and wet soil.png',(16,0,64,48),44)
crop('WetSoil','Farm and Tileset/Tileset/Tilled Soil and wet soil.png',(16,64,64,112),44)
crop('Well','Exterior/Well .png',(0,0,32,48),24,.15)
crop('Chest','Exterior/shipping box.png',(0,0,16,32),24,.15)
crop('Floor','Farm and Tileset/Tileset/Tileset House.png',(272,0,288,16))
crop('Counter','Interior/Tables and desks.png',(128,192,176,224),24,.2)
crop('Clerk','Character and Portrait/Character/Pre-made/Josh/Idle.png',(32,0,64,32),32,.15)
crop('Sign','UI/HUD.png',(112,32,128,48),24)
for i,x in enumerate([16,32,48,64,80]):crop('Crop'+str(i),'Farm Crops/Spring/Carrot.png',(x,0,x+16,16),20,.3)
for action,count in [('Idle',2),('Walk',4)]:
 for row in range(3):
  for col in range(count):crop(f'Deer{action}{row}_{col}',f'Forest Animals/Deer/Male/{action}.png',(col*32,row*32,(col+1)*32,(row+1)*32),32,.2)
for row in range(3):
 for col in range(4):crop(f'Chicken{row}_{col}','Farm Animals/Chicken/Chicken White.png',(col*16,row*16,(col+1)*16,(row+1)*16),24,.2)
for action in ['Idle','Walk']:
 p='Enemy/Sprout Slime/Blue/'+action+'.png';im=Image.open(root/p);print(p,im.size)
 for col in range(im.width//32):crop('Slime'+action+str(col),p,(col*32,0,(col+1)*32,32),32,.2)
(out/'manifest.json').write_text(json.dumps(dict(entries=entries),indent=2),encoding='utf8')
print('Exported',len(entries),'original sprites')
