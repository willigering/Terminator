# Python-NACHBAU (keine Windows-Screenshots) von Terminator 3.6
from compose36 import render
from PIL import Image, ImageDraw, ImageFont
F=ImageFont.truetype('app/BarlowSemiCondensed-Medium.ttf',26)
D=Image.open('v36/new.png').convert('RGBA')
idle=render({}); chg=render({'quick':'15','num':15,'time':'22:45','status':'AKTIV – Herunterfahren um 22:45 Uhr','digits':'001500'}); d=render({'digits':'012345'})
def lab(im,t):
    c=Image.new('RGBA',(im.width,im.height+40),(18,18,20,255)); c.paste(im,(0,40)); ImageDraw.Draw(c).text((10,6),t,font=F,fill=(240,240,240)); return c
z=lambda im,b,s=1:im.crop(b).resize((int((b[2]-b[0])*s),int((b[3]-b[1])*s)),Image.LANCZOS)
S=(1152,767)
rows=[[lab(D.resize(S),'Design (Vorlage)'),lab(idle.resize(S),'3.6 Leerlauf – Python-Nachbau')],
 [lab(chg.resize(S),'3.6 geändert: 15 MIN, UM 22:45, AKTIV'),lab(d.resize(S),'3.6 Ziffern 01:23:45')],
 [lab(z(D,(180,830,1350,995)),'Design: Leiste 100%'),lab(z(idle,(180,830,1350,995)),'3.6: Leiste 100% (Text live)')],
 [lab(z(D,(180,870,770,990),2),'Design: Leiste links 2x'),lab(z(idle,(180,870,770,990),2),'3.6: Leiste links 2x')],
 [lab(z(D,(600,535,930,630),3),'Design: Anzeige 3x'),lab(z(idle,(600,535,930,630),3),'3.6: Anzeige 3x (7-Segment)')]]
W=max(sum(c.width+20 for c in r) for r in rows); H=sum(max(c.height for c in r)+20 for r in rows)
out=Image.new('RGBA',(W,H),(18,18,20,255)); y=0
for r in rows:
    x=0
    for c in r: out.paste(c,(x,y)); x+=c.width+20
    y+=max(c.height for c in r)+20
out.convert('RGB').save('/workspace/terminator-3.6-states.png'); print(out.size)
for i,(r) in enumerate(rows[2:]): 
    a,b=r; c=Image.new('RGB',(max(a.width,b.width),a.height+b.height)); c.paste(a,(0,0)); c.paste(b,(0,a.height)); c.save(f'v36/chk{i}.png')
