# 3.11: Hintergrund neu interpretiert – linke Szene und rechte Szene (Katze) per GenerateImage
# sauber neu erzeugt (gleiche Komposition), mit weichen Übergängen ins unveränderte Mittelteil
# (Titel, Ring, Anzeige) und die Leiste/Fensterknöpfe eingesetzt.
import numpy as np
from PIL import Image
base=np.asarray(Image.open('v310/new.png').convert('RGB')).astype(np.float32)
def put(path,box,ramps):
    x0,y0,x1,y1=box; w,h=x1-x0,y1-y0
    t=np.asarray(Image.open(path).convert('RGB').resize((w,h),Image.LANCZOS)).astype(np.float32)
    o=base[y0:y1,x0:x1]
    t=(t-t.mean((0,1)))/(t.std((0,1))+1e-3)*(0.5*t.std((0,1))+0.5*o.std((0,1)))+(0.5*t.mean((0,1))+0.5*o.mean((0,1)))  # Ton halb angleichen
    yy,xx=np.mgrid[0:h,0:w].astype(np.float32); m=np.ones((h,w),np.float32)
    for side,n in ramps.items():
        r={'l':xx,'r':w-1-xx,'t':yy,'b':h-1-yy}[side]; m=np.minimum(m,np.clip(r/n,0,1))
    m=m*m*(3-2*m)
    base[y0:y1,x0:x1]=t*m[...,None]+o*(1-m[...,None])
put('v311/Lg.jpg',(0,93,560,840),{'r':150,'t':60,'b':40})
put('v311/Rg.jpg',(1000,124,1537,840),{'l':170,'t':260,'b':40})
Image.fromarray(np.clip(base,0,255).astype(np.uint8)).save('v311/new.png')
