"""Preview of exact original atlas frames used by the runtime library."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path('Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Character and Portrait/Character/Pre-made/Josh')
out=Path('Design/Validation/PlayerAnimations')
items=[('Caminar','Walk.png',32),('Correr','Run.png',32),('Espada','Sword.png',32),('Arco','Bow and Arrow.png',32),('Plantar','Throwing items.png',32),('Regar','Watering.png',32),('Recoger','Pick Up Itens/Pick Up Itens.png',64),('Pala','Shovel.png',32),('Hacha','Axe.png',32),('Pico','Pickaxe.png',32),('Azada','Hoe.png',32),('Daño','Damage.png',32)]
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
frames=[]
for tick in range(64):
 im=Image.new('RGB',(768,590),'#24372d');d=ImageDraw.Draw(im)
 d.text((22,14),'PLAYER · ANIMACIONES ORIGINALES',font=font,fill='#ffe4ba')
 face=(tick//16)%4;row=min(face,2)
 d.text((22,43),['Frente','Espalda','Izquierda','Derecha'][face]+' · 54 secuencias en la biblioteca',font=small,fill='#b6c7ae')
 for i,(label,path,size) in enumerate(items):
  atlas=Image.open(root/path).convert('RGBA');count=atlas.width//size;f=tick%count
  piece=atlas.crop((f*size,row*size,(f+1)*size,(row+1)*size))
  normalized=Image.new('RGBA',(64,64));normalized.alpha_composite(piece,((64-size)//2,(64-size)//2))
  if face==3:normalized=normalized.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
  normalized=normalized.resize((160,160),Image.Resampling.NEAREST)
  x=(i%4)*192;y=76+(i//4)*168
  d.rounded_rectangle((x+8,y,x+184,y+160),radius=8,fill='#405741')
  im.paste(normalized,(x+16,y-9),normalized)
  d.text((x+18,y+131),label,font=font,fill='#ffe4ba')
 frames.append(im)
frames[0].save(out/'player-animaciones.gif',save_all=True,append_images=frames[1:],duration=100,loop=0,optimize=False)
frames[5].save(out/'player-animaciones.png')
