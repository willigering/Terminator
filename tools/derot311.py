# 3.7 "weniger rot": kräftige Rottöne im Hintergrund ~35 % entsättigen/abdunkeln.
# Orange (Sonnenuntergang) bleibt, das rote "!" im Titel bleibt.
import numpy as np
from PIL import Image, ImageFilter, ImageDraw
im=Image.open('v311/new.png').convert('RGB'); a=np.asarray(im).astype(np.float32)/255
hsv=np.asarray(im.convert('HSV')).astype(np.float32)/255
h=hsv[...,0]*360; s=hsv[...,1]; v=hsv[...,2]
d=np.minimum(np.abs(h),np.abs(360-h))            # Abstand zu Rot (0°)
wh=np.clip((24-d)/10,0,1)                         # voll bis 14°, aus ab 24° (Orange bleibt)
ws=np.clip((s-0.30)/0.25,0,1)                     # nur kräftige Töne
w=wh*ws
m=Image.new('L',im.size,0); ImageDraw.Draw(m).rectangle((1048,112,1102,222),fill=255)
keep=np.asarray(m.filter(ImageFilter.GaussianBlur(4))).astype(np.float32)/255
w=w*(1-keep)*0.36
grey=a.mean(axis=2,keepdims=True)
out=a*(1-w[...,None])+grey*w[...,None]           # Richtung Grau
out=out*(1-0.10*w[...,None]/0.36)                 # etwas dunkler
Image.fromarray((np.clip(out,0,1)*255+0.5).astype(np.uint8)).save('v311/derot.png')
