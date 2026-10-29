# 3.15: Titel in Großbuchstaben ohne "!" (gleiches Chrom-Lettering, per GenerateImage), weich ins Bild gesetzt
import numpy as np
from PIL import Image
base=np.asarray(Image.open('v311/new.png').convert('RGB')).astype(np.float32)
x0,y0=380,20; t=np.asarray(Image.open('v315/title_gen.jpg').convert('RGB').resize((800,450),Image.LANCZOS)).astype(np.float32)
o=base[y0:y0+450,x0:x0+800]
H,W=450,800; yy,xx=np.mgrid[0:H,0:W].astype(np.float32)
m=np.clip((200-yy)/14,0,1)*np.clip(xx/30,0,1)*np.clip((W-1-xx)/30,0,1)*np.clip((yy+20)/20,0,1)
m=m*m*(3-2*m)
base[y0:y0+H,x0:x0+W]=t*m[...,None]+o*(1-m[...,None])
Image.fromarray(np.clip(base,0,255).astype(np.uint8)).save('v315/new.png')
