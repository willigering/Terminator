# Erzeugt aus dem finalen Design (1537x1023) alle Assets für Terminator 3.6:
# design.png (unverändert), sprites.png (Atlas) und Sprites.cs (Koordinaten).
from PIL import Image
import numpy as np, cv2, json
SRC='v37/new.png'
img=Image.open(SRC).convert('RGB'); A=np.asarray(img).astype(np.float32)
H,W=A.shape[:2]; K=W/960.0   # Quellpixel je logischem Pixel (uniform, x und y gleich)
lum=lambda x: x[...,:3].mean(-1)
R1Y=(877,919); R2Y=(932,981)
BTN={'in':(195,271),'unit':(414,499),'q15':(515,604),'q30':(620,721),'q60':(736,827),'q120':(844,934),'at':(952,1039)}
FIELD={'num':(299,397,R1Y),'time':(1055,1174,R1Y),'empty':(1189,1331,R1Y),'act':(195,603,R2Y),'start':(620,935,R2Y),'cancel':(952,1331,R2Y)}
M=10  # Rand um jeden Knopf (Glühen)
sprites={}  # name -> (RGBA array, (sx,sy) Zielposition in Quellpixeln)
def crop(x0,y0,x1,y1): return A[y0:y1,x0:x1].copy()
def flatten(c, inset_x, inset_y, x_from=None, x_to=None):
    # Text entfernen: jede Zeile im Innenbereich durch den Zeilen-Median ersetzen
    c=c.copy(); h,w=c.shape[:2]
    xa=inset_x if x_from is None else x_from; xb=w-inset_x if x_to is None else x_to
    for y in range(inset_y,h-inset_y):
        row=c[y,xa:xb]; med=np.median(row,axis=0); c[y,xa:xb]=med
    return c
def btn_crop(n):
    x0,x1=BTN[n]; return crop(x0-M,R1Y[0]-M,x1+1+M,R1Y[1]+1+M), (x0-M,R1Y[0]-M)
def slice3(t, neww, s=M+8):
    h,w=t.shape[:2]
    mid=cv2.resize(t[:,s:w-s],(neww-2*s,h),interpolation=cv2.INTER_LINEAR)
    return np.concatenate([t[:,:s],mid,t[:,w-s:]],1)
INS=M+4
def text_alpha(src, tmpl):
    d=lum(src)-lum(tmpl); d[d<0]=0
    m=np.zeros_like(d); m[INS:-INS,INS:-INS]=1; d*=m
    mx=np.percentile(d[d>0],99) if (d>0).any() else 1
    return np.clip(d/mx,0,1)
def textcolor(src,al):
    sel=al>0.85; return np.median(src[sel],0) if sel.any() else np.array([220,220,220.])
def comp(t, al, col, dx=0):
    al=np.roll(al,dx,1)[...,None]; return t*(1-al)+col*al
def tmpl_of(n, ledfree=False):
    c,_=btn_crop(n); return flatten(c, INS, M+3)
def add(name, arr, pos, alpha=None):
    rgba=np.dstack([np.clip(arr,0,255), (np.full(arr.shape[:2],255.) if alpha is None else alpha*255)]).astype(np.uint8)
    sprites[name]=(rgba,pos)
# --- Kopien aus dem Design
for n in BTN:
    c,p=btn_crop(n); add(n+('_on' if n in ('q30','in') else '_off'), c, p)
greyT=tmpl_of('q15'); redT=tmpl_of('q30')
inArt,_=btn_crop('in'); inT=flatten(inArt, INS, M+3)
def synth(target, base_t, text_src, color_from, alpha_override=None, dx_extra=0):
    x0,x1=BTN[target]; w=x1+1-x0+2*M
    t=slice3(base_t,w)
    src,_=btn_crop(text_src); st=flatten(src,INS,M+3)
    al=alpha_override if alpha_override is not None else text_alpha(src,st)
    # Text zentriert in den Zielknopf
    ys,xs=np.nonzero(al>0.05); cx_src=(xs.min()+xs.max())/2; 
    out=np.zeros((t.shape[0],w)); off=int(round(w/2 - (cx_src) + dx_extra))
    for x in range(al.shape[1]):
        tx=x+off
        if 0<=tx<w: out[:,tx]=al[:,x]
    col=color_from
    return comp(t,out,col)
gc=textcolor(*[(lambda c:(c,text_alpha(c,flatten(c,INS,M+3))))(btn_crop('q15')[0])][0])
rc=textcolor(*[(lambda c:(c,text_alpha(c,flatten(c,INS,M+3))))(btn_crop('q30')[0])][0])
print('grey text',gc,'red text',rc)
for n in ('q15','q60','q120','at'): add(n+'_on', synth(n, redT, n, rc), (BTN[n][0]-M,R1Y[0]-M))
add('q30_off', synth('q30', greyT, 'q30', gc), (BTN['q30'][0]-M,R1Y[0]-M))
# STD (Einheit): "STD" aus "1 STD"
src,_=btn_crop('q60'); al=text_alpha(src,flatten(src,INS,M+3)); cols=np.nonzero(al.max(0)>0.1)[0]
gaps=[i for i in range(cols.min(),cols.max()) if al[:,i].max()<0.05]
cut=max(gaps) if gaps else cols.min()
# erste Lücke nach der "1"
g1=[i for i in gaps if i>cols.min()+2]; cut=g1[0] if g1 else cut
al2=al.copy(); al2[:,:cut]=0
add('unit_std', synth('unit', greyT, 'q60', gc, alpha_override=al2), (BTN['unit'][0]-M,R1Y[0]-M))
# IN aus: grauer Knopf, LED dunkel, Text grau
al_in=text_alpha(inArt,inT)
x0,x1=BTN['in']; w=x1+1-x0+2*M
t=slice3(greyT,w)
# LED-Bereich: kräftig rote Pixel im linken Teil
red=(inArt[...,0]-inArt[...,1]>120)&(inArt[...,0]>200); red[:, w//2:]=False; red[:INS]=False; red[-INS:]=False
ledal=al_in.copy(); ledal[:, :w//2]*= (1-red[:, :w//2]); 
txtal=al_in.copy(); txtal[:, :int(w*0.42)]=0
out=comp(t, txtal, gc)
out_led=t.copy()
ys,xs=np.nonzero(red); 
if len(xs):
    cy,cx=ys.mean(),xs.mean(); yy,xx=np.mgrid[:t.shape[0],:w]; d=np.sqrt((yy-cy)**2+(xx-cx)**2)
    la=np.clip(5.2-d,0,1)[...,None]; out=out*(1-la)+np.array([92,38,36.])*la; out_led=out_led*(1-la)+np.array([92,38,36.])*la
add('in_off', out, (x0-M,R1Y[0]-M))

# --- 3.3: Hintergründe ohne Text für jeden Knopf und Zustand (Text wird live gezeichnet)
for n in BTN:
    x0,x1=BTN[n]; w=x1+1-x0+2*M; own=tmpl_of(n); pos=(x0-M,R1Y[0]-M)
    if n=='in':
        ledx=int(w*0.42); c,_=btn_crop('in'); onbg=flatten(c,INS,M+3,ledx,w-INS)
        add('in_bg_on', onbg, pos)
        add('in_bg_off', sprites['in_off'][0][...,:3].astype(np.float32)*0+out_led, pos)
        continue
    on_art = n=='q30'
    add(n+'_bg_on', own if on_art else slice3(redT,w), pos)
    add(n+'_bg_off', slice3(greyT,w) if on_art else own, pos)
# --- Felder: Text weg (Spinner/Pfeil bleibt)
def fld(n): x0,x1,(y0,y1)=FIELD[n]; return crop(x0-M,y0-M,x1+1+M,y1+1+M),(x0-M,y0-M)
c,p=fld('num'); add('num_clean', flatten(c, INS, M+3, INS, c.shape[1]-M-30), p)
c,p=fld('time'); add('time_clean', flatten(c, INS, M+3, INS, c.shape[1]-M-30), p)
c,p=fld('act'); add('act_clean', flatten(c, INS, M+3, INS, c.shape[1]-M-50), p)
c,p=fld('cancel'); add('cancel_clean', flatten(c, INS, M+3), p)
c,p=fld('start'); add('start_clean', flatten(c, INS, M+3), p)
# Statuszeile ohne Text
def inpaint(x0,y0,x1,y1, maskfn, scale=1):
    reg=A[y0:y1,x0:x1].astype(np.uint8); m=maskfn(reg).astype(np.uint8)*255
    m=cv2.dilate(m,np.ones((3,3),np.uint8),2)
    bgr=cv2.cvtColor(reg,cv2.COLOR_RGB2BGR)
    if scale!=1:
        s=cv2.resize(bgr,None,fx=scale,fy=scale,interpolation=cv2.INTER_AREA); ms=cv2.resize(m,(s.shape[1],s.shape[0]))
        r=cv2.inpaint(s,(ms>0).astype(np.uint8)*255,5,cv2.INPAINT_TELEA); r=cv2.resize(r,(bgr.shape[1],bgr.shape[0]),interpolation=cv2.INTER_CUBIC)
        bgr=np.where((m>0)[...,None],r,bgr)
    else: bgr=cv2.inpaint(bgr,m,4,cv2.INPAINT_TELEA)
    return cv2.cvtColor(bgr,cv2.COLOR_BGR2RGB).astype(np.float32)
st=(552,836,972,864)
txtmask=lambda r: (r.astype(int).max(2)>60)
add('status_clean', inpaint(*st, txtmask), st[:2])
# --- 3.6: Anzeige ohne Ziffern (Ziffern werden prozedural gezeichnet)
DX0,DY0,DX1,DY1=628,548,898,602
reg=A[DY0:DY1,DX0:DX1]
yy,xx=np.mgrid[:DY1-DY0,:DX1-DX0]; Y=yy+DY0; X=xx+DX0
inplate=(Y>=553)&(Y<=598)&(X>=638)&(X<=890)
dm=(reg[...,0]>60)&(reg[...,0]-reg[...,1]>28)&inplate
dm8=cv2.dilate(dm.astype(np.uint8)*255,np.ones((3,3),np.uint8),2)
clean=cv2.cvtColor(cv2.inpaint(cv2.cvtColor(reg.astype(np.uint8),cv2.COLOR_RGB2BGR),dm8,3,cv2.INPAINT_TELEA),cv2.COLOR_BGR2RGB).astype(np.float32)
h,w=clean.shape[:2]; d=np.minimum(np.minimum(xx+.5,w-xx-.5),np.minimum(yy+.5,h-yy-.5)); fa=np.clip(d/6,0,1); fa=fa*fa*(3-2*fa)
add('panel',clean,(DX0,DY0),fa)
origins=[0]*6; oy=0; ox=0; colon_x=[0,0]
# --- Atlas schreiben (nur textfreie Sprites)
for n in list(sprites):
    if n.endswith(('_on','_off','_std')) and '_bg_' not in n: del sprites[n]
names=list(sprites); pad=2; aw=1600; x=y=rowh=0; place={}
for n in sorted(names,key=lambda n:-sprites[n][0].shape[0]):
    arr=sprites[n][0]; hh,ww=arr.shape[:2]
    if x+ww>aw: x=0; y+=rowh+pad; rowh=0
    place[n]=(x,y,ww,hh); x+=ww+pad; rowh=max(rowh,hh)
atlas=np.zeros((y+rowh,aw,4),np.uint8)
for n,(x,y,ww,hh) in place.items(): atlas[y:y+hh,x:x+ww]=sprites[n][0]
Image.fromarray(atlas,'RGBA').save('app/sprites.png',optimize=True)
img.save('app/design.png',optimize=True)
cs=['// automatisch erzeugt von gen36.py aus dem finalen Design (1537x1023). Nicht von Hand ändern.',
'using System.Drawing;','using System.Collections.Generic;','static class Sprites','{',
'    public const float K = %rf; // Quellpixel je logischem Pixel'%K,
'    public const int SrcW = %d, SrcH = %d;'%(W,H),
'    public static readonly Dictionary<string, Rectangle> Atlas = new Dictionary<string, Rectangle> {']
for n,(x,y,ww,hh) in place.items(): cs.append('        { "%s", new Rectangle(%d, %d, %d, %d) },'%(n,x,y,ww,hh))
cs.append('    };'); cs.append('    // Zielposition im Design in Quellpixeln')
cs.append('    public static readonly Dictionary<string, Point> Pos = new Dictionary<string, Point> {')
for n in place: cs.append('        { "%s", new Point(%d, %d) },'%(n,*sprites[n][1]))
cs.append('    };')
cs.append('    public static readonly int[] DigitX = { %s }; public const int DigitY = %d; // Ursprung je Ziffer (Quellpixel)'%(', '.join(str(o) for o in origins), oy))
cs.append('    public const int DigitX0 = %d; // Ursprung der Vorlagen-Ziffer'%ox)
cs.append('    public static readonly int[] ColonX = { %s };'%(', '.join(str(c) for c in colon_x)))
def L(x0,x1,y0,y1): return 'new RectangleF(%.2ff, %.2ff, %.2ff, %.2ff)'%(x0/K,y0/K,(x1+1-x0)/K,(y1+1-y0)/K)
cs.append('    // Knopf-Rechtecke (logisch, 960er Raster)')
for n,(x0,x1) in BTN.items(): cs.append('    public static readonly RectangleF R_%s = %s;'%(n,L(x0,x1,*R1Y)))
for n,(x0,x1,(y0,y1)) in FIELD.items(): cs.append('    public static readonly RectangleF R_%s = %s;'%(n,L(x0,x1,y0,y1)))
cs.append('}')
open('app/Sprites.cs','w').write('\n'.join(cs)+'\n')
print('atlas',atlas.shape, len(place))
