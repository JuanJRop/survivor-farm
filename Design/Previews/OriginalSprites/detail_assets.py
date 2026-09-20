exec(open(__file__.replace('detail_assets.py','inspect_assets.py'),encoding='utf8').read().split('out=Image.new')[0])
items=[('grass',paths[0],(0,0,192,256)),('house',paths[2],(0,352,400,480)),('tree',paths[3],None),('bars',paths[6],None),('hud',paths[4],None),('weapons',paths[9],None),('soil',paths[17],(0,0,192,64)),('slots',paths[7],(0,0,128,160))]
for name,p,box in items:
 im=Image.open(root/p).convert('RGBA')
 if box: im=im.crop(box)
 im=im.resize((im.width*3,im.height*3),Image.Resampling.NEAREST)
 bg=Image.new('RGB',(im.width+30,im.height+24),'#46535d'); bg.paste(im,(30,24),im); d=ImageDraw.Draw(bg)
 for x in range(0,im.width//3,16):
  d.line((x*3+30,24,x*3+30,bg.height),fill='#68757c'); d.text((x*3+30,2),str(x+(box[0] if box else 0)),fill='white')
 for y in range(0,im.height//3,16):
  d.line((30,y*3+24,bg.width,y*3+24),fill='#68757c'); d.text((0,y*3+24),str(y+(box[1] if box else 0)),fill='white')
 bg.save(Path(__file__).with_name('detail-'+name+'.png'))
