# 3.10: graue Britisch Kurzhaar in die Gegenlicht-Szene einbetten (Farbe/Helligkeit an die alte,
# gut integrierte Katze angleichen, Schatten innen, warmes Rim-Light, weiche Kanten, Reflexion/Schatten)
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage as nd
f=lambda p:np.asarray(Image.open(p).convert('RGB')).astype(np.float32)/255
g=f('v310/g.png'); o=f('v39/cat34.png')
mg=np.asarray(Image.open('v310/mg.png')).astype(np.float32)/255; mo=np.asarray(Image.open('v310/mo.png')).astype(np.float32)/255
cg=mg>0.5; co=mo>0.5
# 1) Helligkeit: Luminanz der neuen Katze auf Verteilung der alten (Quantil-Matching), etwas dunkler
Lg=g@[.299,.587,.114]; Lo=o@[.299,.587,.114]
q=np.linspace(0,1,101); src=np.quantile(Lg[cg],q); dst=np.quantile(Lo[co],q)*0.92
Ln=np.interp(Lg,src,dst)
# 2) Farbe: grau behalten (geringe Eigensättigung), mit warmem Szenenlicht einfärben (Farbe der alten Katze je Helligkeit)
chroma=(g-Lg[...,None])*0.35                      # Blaugrau stark reduzieren, Grau bleibt
warm=np.array([1.18,0.92,0.72])                    # Sonnenuntergang-Stich
base=np.clip(Ln[...,None]*warm+chroma,0,1)
# 3) Gegenlicht: innen dunkler, Konturen warm leuchtend (Rim)
dist=nd.distance_transform_edt(cg)
inner=np.clip(dist/22,0,1)
base*=1-0.30*inner[...,None]
rim=np.exp(-dist/2.5)*cg
# Rim stärker zur Sonne hin (rechts oben) und oben
H,W=cg.shape; yy,xx=np.mgrid[0:H,0:W]; side=np.clip(0.55+0.45*(xx/W)+0.25*(1-yy/H),0,1.2)
base=np.clip(base+rim[...,None]*side[...,None]*np.array([1.0,0.55,0.22])*0.55,0,1)
# 4) Weiche, organische Kante
soft=np.asarray(Image.fromarray((mg*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.6))).astype(np.float32)/255
# Hintergrund dort, wo die alte Katze war: generierter Hintergrund (entspricht 3.9), außen Original
uni=np.maximum(mo,mg); uni=np.asarray(Image.fromarray((np.clip(uni*1.0,0,1)*255).astype(np.uint8)).filter(ImageFilter.MaxFilter(7)).filter(ImageFilter.GaussianBlur(5))).astype(np.float32)/255
bg=g*uni[...,None]+o*(1-uni[...,None])
out=base*soft[...,None]+bg*(1-soft[...,None])
# 5) Bodenschatten + Reflexion unter den Pfoten
rows=np.where(cg.any(1))[0]; foot=rows.max()
cols=np.where(cg[foot-25:foot].any(0))[0]; cx=(cols.min()+cols.max())/2; hw=(cols.max()-cols.min())/2+18
sh=np.exp(-(((xx-cx)/hw)**2+((yy-foot+3)/7)**2))
out*=1-0.55*sh[...,None]
refl=np.zeros_like(out); ra=np.zeros((H,W),np.float32)
n=min(H-foot-1,120)
for k in range(1,n):
    sy=foot-int(k*1.6)
    if sy<0: break
    refl[foot+k]=base[sy]; ra[foot+k]=soft[sy]*0.28*(1-k/n)
ra=np.asarray(Image.fromarray((ra*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3))).astype(np.float32)/255
refl=np.stack([nd.gaussian_filter(refl[...,i],(2,4)) for i in range(3)],2)
out=out*(1-ra[...,None])+refl*0.8*ra[...,None]
Image.fromarray((np.clip(out,0,1)*255+.5).astype(np.uint8)).save('v310/comp.png')
full=Image.open('v36/new.png').convert('RGB'); full.paste(Image.open('v310/comp.png'),(1088,250)); full.save('v310/new.png')
