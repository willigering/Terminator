# Vorschau 3.15: Großbuchstaben-Titel ohne "!" + Parallax der fernen Ebene (Python-Nachbau, kein Windows-Screenshot)
import os, math, numpy as np
os.environ['TA']='v315/a315'
from compose37 import render, K
from PIL import Image
run={'quick':'15','num':15,'time':'22:45','status':'AKTIV – Herunterfahren um 22:45 Uhr','digits':'001500'}
r=render(run).convert('RGB'); far=Image.open('v315/a315/far.png').convert('RGBA')
def par(img,dx,dy):
    o=img.copy().convert('RGBA'); sh=far.transform(far.size,Image.AFFINE,(1,0,-dx*K,0,1,-dy*K),Image.BILINEAR)
    o.alpha_composite(sh); return o.convert('RGB')
a=r.resize((768,511),Image.LANCZOS)
b=Image.blend(par(r,-4.5,-2),par(r,4.5,2),0.5).resize((768,511),Image.LANCZOS)  # Doppelbelichtung zeigt den Versatz
c=Image.new('RGB',(1556,511),(18,18,20)); c.paste(a,(0,0)); c.paste(b,(788,0)); c.save('/workspace/terminator-3.15-states.png'); c.save('v315/v315.png')
fr=[]
for i in range(40):
    t=i/40*2*math.pi; fr.append(par(r,4.5*math.sin(t),2*math.sin(t*2)).resize((768,511),Image.LANCZOS))
pal=fr[0].quantize(colors=200,method=Image.MEDIANCUT)
q=[i.quantize(palette=pal,dither=Image.Dither.NONE) for i in fr]
q[0].save('/workspace/terminator-3.15-parallax.gif',save_all=True,append_images=q[1:],duration=60,loop=0,optimize=True)
r.save('v315/run_full.png')
