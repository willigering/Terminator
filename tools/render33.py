from compose33 import render
from PIL import Image, ImageDraw, ImageFont
F=ImageFont.truetype('app/BarlowSemiCondensed-Medium.ttf',26)
idle=render({})
chg=render({'quick':'15','num':15,'time':'22:45','status':'AKTIV – Herunterfahren um 22:45 Uhr','digits':'001500'})
d123=render({'digits':'012345','status':'AKTIV – Herunterfahren um 01:23 Uhr'})
orig=Image.open('app/design.png').convert('RGBA')
def lab(im,t):
    c=Image.new('RGBA',(im.width,im.height+40),(18,18,20,255)); c.paste(im,(0,40)); ImageDraw.Draw(c).text((10,6),t,font=F,fill=(240,240,240)); return c
bar=(180,820,1360,1000); dig=(600,540,940,640)
z=lambda im,b,s: im.crop(b).resize(((b[2]-b[0])*s,(b[3]-b[1])*s),Image.LANCZOS)
rows=[lab(idle.crop((0,0,1538,1023)).resize((769,512)),'Leerlauf (00:30:00) – Python-Nachbau'),
      lab(chg.resize((769,512)),'Geändert: 15 MIN, UM 22:45, AKTIV – Python-Nachbau'),
      lab(d123.resize((769,512)),'Ziffern 01:23:45 – Python-Nachbau')]
top=Image.new('RGBA',(769*3,552),(18,18,20,255))
for i,r in enumerate(rows): top.paste(r,(769*i,0))
crops=[lab(z(orig,bar,1),'Design-Original: Leiste'),lab(z(idle,bar,1),'3.3 Leerlauf: Leiste (alles live in Barlow Medium)'),lab(z(chg,bar,1),'3.3 Geändert: Leiste (gleiche Schrift)')]
mid=Image.new('RGBA',(2307,sum(c.height for c in crops)),(18,18,20,255)); y=0
for c in crops: mid.paste(c,(0,y)); y+=c.height
dz=[lab(z(orig,dig,3),'Design-Original Ziffern (3x)'),lab(z(idle,dig,3),'3.3 00:30:00 (3x)'),lab(z(d123,dig,3),'3.3 01:23:45 (3x)')]
bot=Image.new('RGBA',(2307,dz[0].height),(18,18,20,255))
for i,c in enumerate(dz): bot.paste(c.resize((c.width*3//4,c.height*3//4)),(i*769,0))
out=Image.new('RGBA',(2307,top.height+mid.height+bot.height),(18,18,20,255))
out.paste(top,(0,0)); out.paste(mid,(0,top.height)); out.paste(bot,(0,top.height+mid.height))
out.convert('RGB').save('/workspace/terminator-3.3-states.png')
z(chg,(180,800,1360,1000),2).save('/tmp/c_bar.png'); z(idle,(180,800,1360,1000),2).save('/tmp/i_bar.png'); z(orig,(180,800,1360,1000),2).save('/tmp/o_bar.png')
