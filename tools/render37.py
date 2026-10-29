# Python-NACHBAU (keine Windows-Screenshots): 3.6 vs 3.7 nebeneinander
import os, sys, subprocess, pickle
from PIL import Image, ImageDraw, ImageFont
if len(sys.argv)>1:
    os.environ['TA']=sys.argv[1]; from compose37 import render
    st=[{},{'quick':'15','num':15,'time':'22:45','status':'AKTIV – Herunterfahren um 22:45 Uhr','digits':'001500'}]
    for i,s in enumerate(st): render(s).save(f'{sys.argv[1]}/st{i}.png')
    sys.exit()
for a in ('v37/a36','v37/a37'): subprocess.run([sys.executable,__file__,a],check=True,env={**os.environ,'TA':a})
F=ImageFont.truetype('app/BarlowSemiCondensed-Medium.ttf',26)
def lab(im,t):
    c=Image.new('RGBA',(im.width,im.height+40),(18,18,20,255)); c.paste(im,(0,40)); ImageDraw.Draw(c).text((10,6),t,font=F,fill=(240,240,240)); return c
z=lambda im,b,s=1:im.crop(b).resize((int((b[2]-b[0])*s),int((b[3]-b[1])*s)),Image.LANCZOS)
S=(1152,767); I={a:[Image.open(f'v37/{a}/st{i}.png') for i in (0,1)] for a in ('a36','a37')}
rows=[[lab(I['a36'][0].resize(S),'3.6 Leerlauf (Python-Nachbau)'),lab(I['a37'][0].resize(S),'3.7 Leerlauf – weniger rot (Python-Nachbau)')],
 [lab(I['a36'][1].resize(S),'3.6 aktiv: 15 MIN, 22:45'),lab(I['a37'][1].resize(S),'3.7 aktiv: 15 MIN, 22:45')],
 [lab(z(I['a36'][1],(500,500,1000,640),2),'3.6 Anzeige/Status 2x'),lab(z(I['a37'][1],(500,500,1000,640),2),'3.7 Anzeige/Status 2x')],
 [lab(z(I['a36'][1],(180,830,1350,995)),'3.6 Leiste'),lab(z(I['a37'][1],(180,830,1350,995)),'3.7 Leiste')]]
W=max(sum(c.width+20 for c in r) for r in rows); H=sum(max(c.height for c in r)+20 for r in rows)
out=Image.new('RGBA',(W,H),(18,18,20,255)); y=0
for r in rows:
    x=0
    for c in r: out.paste(c,(x,y)); x+=c.width+20
    y+=max(c.height for c in r)+20
out.convert('RGB').save('/workspace/terminator-3.7-states.png'); print(out.size)
