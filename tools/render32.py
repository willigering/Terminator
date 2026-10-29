from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np, math
from compose32 import render, spr, AT, POS
K=1538/960
design=Image.open('v32/src.png').convert('RGB')
# Leerlauf (Standardzustand) – genau wie die App ihn zeichnet: Bild + Zustands-Ausschnitte
idle=render({}).convert('RGB')
# Laufzustand zur Kontrolle: Ring-Blöcke (vereinfachter Nachbau von Ring.Draw), Ziffern aus Segmenten, Status
run=render({'digits':'001247','running':True,'quick':'15'})
d=ImageDraw.Draw(run,'RGBA'); cx,cy,r1,r2=479*K,293.4*K,102*K,119*K
for i in range(60):
    if i>=13: continue
    a=(i+0.5)*2*math.pi/60; h=0.62*math.pi/60
    pts=[(cx+r*math.sin(t),cy-r*math.cos(t)) for r,t in ((r1,a-h),(r2,a-h),(r2,a+h),(r1,a+h))]
    v=0.3+0.7*math.exp(-(13-i)/7); d.polygon(pts,fill=(int(55+200*v**.5),int(9+80*v*v),8))
spr(run,'panel')
from compose32 import SEG, DX, DX0, CX
for i,ch in enumerate('001247'):
    for s in SEG[ch]: spr(run,'seg_'+s,DX[i]-DX0,0)
for c in CX:
    x,y,w,h=AT['colon']; from compose32 import atlas; run.alpha_composite(atlas.crop((x,y,x+w,y+h)),(c,POS['colon'][1]))
spr(run,'status_clean')
f=ImageFont.truetype('fonts/BarlowSemiCondensed-Regular.ttf',int(14*K))
t1='AKTIV'; t2=' – Herunterfahren um 21:47 Uhr'; w1=d.textlength(t1,font=f); w2=d.textlength(t2,font=f); x=475.6*K-(w1+w2)/2
d.text((x,531.5*K),t1,font=f,fill=(238,72,62),anchor='lm'); d.text((x+w1,531.5*K),t2,font=f,fill=(226,226,228),anchor='lm')
run=run.convert('RGB')
idle.save('/workspace/terminator-3.2-preview.png'); run.save('/workspace/terminator-3.2-running-preview.png')
diff=np.abs(np.asarray(idle).astype(int)-np.asarray(design).astype(int)).max(2)
print('idle diff max',diff.max(),'mean',diff.mean(),'pixels>8',(diff>8).sum())
heat=np.clip(diff*8,0,255).astype(np.uint8)
hm=Image.fromarray(np.dstack([heat,np.zeros_like(heat),np.zeros_like(heat)]))
W,H=design.size; s=0.5; w,h=int(W*s),int(H*s)
c=Image.new('RGB',(w*3+40,h+60),(22,22,22)); dd=ImageDraw.Draw(c); fo=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',18)
for i,(im,l) in enumerate(((design,'Design (Original)'),(idle,'Terminator 3.2 (Nachbau der Zeichenlogik, Leerlauf)'),(hm,'Pixel-Differenz x8 (max %d, Mittel %.2f)'%(diff.max(),diff.mean())))):
    c.paste(im.resize((w,h),Image.LANCZOS),(10+i*(w+10),45)); dd.text((10+i*(w+10),14),l,font=fo,fill='white')
c.save('/workspace/terminator-3.2-compare.png'); print(c.size)
