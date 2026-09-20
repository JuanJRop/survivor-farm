from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[3]
pack=root/'Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack'
out=root/'Assets/SurvivorFarm/UI/OriginalSprites'
out.mkdir(parents=True,exist_ok=True)
items={'Panel':('UI/Inventory/inventory.png',(0,64,48,112)),
'Slot':('UI/Inventory/Slots.png',(8,8,44,36)),
'Heart':('UI/Bars.png',(0,0,16,16)), 'EmptyHeart':('UI/Bars.png',(32,0,48,16)),
'HungerFrame':('UI/Bars.png',(144,100,188,108)),
'HungerFill':('UI/Bars.png',(52,101,78,106)),
'Coin':('UI/Money.png',(0,0,16,16)),
'Backpack':('UI/HUD.png',(48,32,64,48)),
'Hand':('UI/HUD.png',(0,48,16,64)),
'Quest':('UI/HUD.png',(112,32,128,48))}
for name,x in zip(['Sword','Bow','Axe','Pickaxe','Hoe','Shovel','WateringCan'],[32,64,48,16,96,112,0]):
 items[name]=('Icons/Weapons/RPG/1.png',(x,0,x+16,16))
for name,(path,box) in items.items():
 Image.open(pack/path).crop(box).save(out/(name+'.png'))
print('Exported',len(items),'original sprite crops')
