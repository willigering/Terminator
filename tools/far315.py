# 3.15 Parallax: Ferne-Ebene (Himmel + ferne Skyline) als ARGB-Maske aus design.png; Titel, Ring, Katze, Leiste bleiben fest
import numpy as np
from PIL import Image, ImageFilter
from rembg import remove, new_session
K=1537/960
d=Image.open('app/design.png').convert('RGB'); W,H=d.size
yy,xx=np.mgrid[0:H,0:W].astype(np.float32); L=lambda v:v*K
m=np.clip((L(330)-yy)/L(40),0,1)                       # Horizont weich
ring=np.hypot(xx-L(478),yy-L(300))-L(182); m*=np.clip(ring/L(10),0,1)
def box(x0,y0,x1,y1,f):
    dx=np.maximum(np.maximum(L(x0)-xx,xx-L(x1)),0); dy=np.maximum(np.maximum(L(y0)-yy,yy-L(y1)),0)
    return np.clip(np.hypot(dx,dy)/L(f),0,1)
m*=box(255,22,690,148,10)      # Titel
m*=box(870,0,960,40,6)         # Fensterknöpfe
cx0,cy0,cx1,cy1=[int(L(v)) for v in (740,215,935,500)]
cat=remove(d.crop((cx0,cy0,cx1,cy1)),session=new_session('u2net')).split()[3]
cat=cat.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(4))
cm=np.zeros((H,W),np.float32); cm[cy0:cy1,cx0:cx1]=np.asarray(cat)/255; m*=1-cm
a=(m*255+.5).astype(np.uint8)
out=d.convert('RGBA'); out.putalpha(Image.fromarray(a)); out.crop((0,0,W,int(L(336)))).save('app/far.png',optimize=True)
Image.fromarray(a).resize((480,320)).save('v315/farmask.png')
