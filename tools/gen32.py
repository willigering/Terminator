# Erzeugt aus dem Original-Design (1538x1023) alle Assets für Terminator 3.2:
# design.png (unverändert), sprites.png (Atlas) und Sprites.cs (Koordinaten).
from PIL import Image
import numpy as np, cv2, json
SRC='v32/src.png'
img=Image.open(SRC).convert('RGB'); A=np.asarray(img).astype(np.float32)
H,W=A.shape[:2]; K=W/960.0   # Quellpixel je logischem Pixel (uniform, x und y gleich)
lum=lambda x: x[...,:3].mean(-1)
R1Y=(879,920); R2Y=(932,979)
BTN={'in':(200,274),'unit':(417,501),'q15':(518,606),'q30':(623,721),'q60':(738,828),'q120':(845,935),'at':(954,1038)}
FIELD={'num':(304,400,R1Y),'time':(1055,1173,R1Y),'empty':(1189,1327,R1Y),'act':(200,605,R2Y),'start':(623,935,R2Y),'cancel':(954,1327,R2Y)}
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
ys,xs=np.nonzero(red); 
if len(xs):
    cy,cx=ys.mean(),xs.mean(); yy,xx=np.mgrid[:t.shape[0],:w]; d=np.sqrt((yy-cy)**2+(xx-cx)**2)
    la=np.clip(5.2-d,0,1)[...,None]; out=out*(1-la)+np.array([92,38,36.])*la
add('in_off', out, (x0-M,R1Y[0]-M))
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
st=(560,834,980,866)
txtmask=lambda r: (r.astype(int).max(2)>60)
add('status_clean', inpaint(*st, txtmask), st[:2])
# Titel ohne Text (Bereich wie in 3.1, nur in Quellpixeln)
tt=(int(258*K),0,int(712*K),int(152*K*1023/1024.)+4)
full=lambda r: np.ones(r.shape[:2],bool)
def soft_inpaint(x0,y0,x1,y1, pad=10, scale=0.5):
    X0,Y0,X1,Y1=max(0,x0-pad),max(0,y0-pad),min(W,x1+pad),min(H,y1+pad)
    reg=cv2.cvtColor(A[Y0:Y1,X0:X1].astype(np.uint8),cv2.COLOR_RGB2BGR)
    m=np.zeros(reg.shape[:2],np.uint8); m[y0-Y0:y1-Y0, x0-X0:x1-X0]=255
    s=cv2.resize(reg,None,fx=scale,fy=scale,interpolation=cv2.INTER_AREA); ms=cv2.resize(m,(s.shape[1],s.shape[0]),interpolation=cv2.INTER_NEAREST)
    r=cv2.inpaint(s,ms,7,cv2.INPAINT_TELEA); r=cv2.resize(r,(reg.shape[1],reg.shape[0]),interpolation=cv2.INTER_CUBIC)
    r=cv2.GaussianBlur(r,(0,0),1.0)
    noise=np.random.default_rng(1).normal(0,2.2,r.shape)
    r=np.clip(r+noise,0,255)
    f=cv2.GaussianBlur(m.astype(np.float32)/255,(0,0),3)[...,None]
    out=reg*(1-f)+r*f
    return cv2.cvtColor(out.astype(np.uint8),cv2.COLOR_BGR2RGB).astype(np.float32),(X0,Y0)
arr,p=soft_inpaint(413,6,1141,240); add('title_clean',arr,p)
arr,p=soft_inpaint(522,743,1019,792,pad=8,scale=1); add('sub_clean',arr,p)
# --- Anzeige: Platten ohne Ziffern (weicher Rand) + Segmente aus den Original-Ziffern
DX0,DY0,DX1,DY1=612,556,928,628
reg=A[DY0:DY1,DX0:DX1]
yy,xx=np.mgrid[:DY1-DY0,:DX1-DX0]; Y=yy+DY0; X=xx+DX0
inplate=(Y>=566)&(Y<=616)&(X>=636)&(X<=898)
dm=(reg[...,0]>70)&(reg[...,0]-reg[...,1]>30)&inplate
dm8=cv2.dilate(dm.astype(np.uint8)*255,np.ones((3,3),np.uint8),2)
clean=cv2.cvtColor(cv2.inpaint(cv2.cvtColor(reg.astype(np.uint8),cv2.COLOR_RGB2BGR),dm8,3,cv2.INPAINT_TELEA),cv2.COLOR_BGR2RGB).astype(np.float32)
h,w=clean.shape[:2]; d=np.minimum(np.minimum(xx+.5,w-xx-.5),np.minimum(yy+.5,h-yy-.5)); fa=np.clip(d/7,0,1); fa=fa*fa*(3-2*fa)
add('panel',clean,(DX0,DY0),fa)
# Leuchtanteil: Original = Platte*(1-a) + Leuchtfarbe*a  (Leuchtfarbe = hellste Ziffernfarbe)
glow=np.array([255,120,95.])
den=np.clip(glow-clean,1,None)
al=np.clip(((reg-clean)/den).max(-1),0,1)*(dm8>0)
glyph=al>0.25
runs=[];st_=None
for x in range(w+1):
    v= x<w and glyph[:,x].any()
    if v and st_ is None: st_=x
    if not v and st_ is not None: runs.append((st_,x-1)); st_=None
digits=[r for r in runs if r[1]-r[0]>10]; colons=[r for r in runs if r[1]-r[0]<=10]
rows=np.nonzero(glyph.any(1))[0]
print('digits',[(r[0]+DX0,r[1]+DX0) for r in digits],'colons',[(r[0]+DX0,r[1]+DX0) for r in colons],'rows',rows.min()+DY0,rows.max()+DY0)
# Farbe der Segmente (unvormultipliziert)
def seg_sprite(mask_al):
    a_=mask_al[...,None]; col=np.where(a_>0.02,(reg-clean*(1-a_))/np.clip(a_,0.02,None),glow); col=np.clip(col,0,255)
    return col
json_out={'K':K}
SL=0.14
gy0,gy1=rows.min(),rows.max(); gh=gy1-gy0
def classify(r):  # r = (x0,x1) eines Ziffernlaufs; liefert Segmentmasken (Glyphe "0" bzw. "3")
    x0,x1=r; sub=np.zeros_like(al); sub[:,max(0,x0-3):x1+4]=al[:,max(0,x0-3):x1+4]
    ys,xs=np.mgrid[:h,:w]; cy=(gy0+gy1)/2; xp=xs+SL*(ys-cy); cxp=(x0+x1)/2
    yr=(ys-gy0)/gh
    segs={}
    segs['a']=sub*(yr<0.2); segs['d']=sub*(yr>0.8); segs['g']=sub*((yr>0.38)&(yr<0.62))
    mid=(yr>=0.2)&(yr<=0.8)
    segs['f']=sub*mid*(xp<cxp)*(yr<0.5); segs['e']=sub*mid*(xp<cxp)*(yr>=0.5)
    segs['b']=sub*mid*(xp>=cxp)*(yr<0.5); segs['c']=sub*mid*(xp>=cxp)*(yr>=0.5)
    return segs
z=classify(digits[0])      # erste "0"
t=classify(digits[2])      # die "3"
segmasks={k:z[k] for k in 'abcdef'}; segmasks['g']=t['g']*((np.mgrid[:h,:w][1]>=digits[2][0]+2))
# g aus der 3 auf die Position der ersten Ziffer verschieben (rechte Kanten fluchten)
shift=digits[2][1]-digits[0][1]
segmasks['g']=np.roll(segmasks['g'],-shift,1)
gcol=seg_sprite(t['g']); gcol=np.roll(gcol,-shift,1)
ox,oy=DX0+digits[0][0]-3, DY0+gy0-3
seg_rects={}
for kk,m in segmasks.items():
    col=gcol if kk=='g' else seg_sprite(m)
    ys,xs=np.nonzero(m>0.01)
    y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
    add('seg_'+kk, col[y0:y1,x0:x1], (DX0+x0,DY0+y0), m[y0:y1,x0:x1])
    seg_rects[kk]=(DX0+x0-ox, DY0+y0-oy)
# Ziffern-Ursprünge: rechte Kanten der Läufe minus Breite der "0"
w0=digits[0][1]-digits[0][0]
origins=[DX0+r[1]-w0-3 for r in digits]
print('origins',origins, 'ox',ox)
# Doppelpunkte (beide gleich, einer als Sprite)
c0=colons[0]; cm=np.zeros_like(al); cm[:,c0[0]-2:c0[1]+3]=al[:,c0[0]-2:c0[1]+3]
ys,xs=np.nonzero(cm>0.01); y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
add('colon', seg_sprite(cm)[y0:y1,x0:x1], (DX0+x0,DY0+y0), cm[y0:y1,x0:x1])
colon_x=[DX0+c[0]-2+(x0-(c0[0]-2)) for c in colons]
# --- Atlas schreiben
names=list(sprites); pad=2; aw=1600; x=y=rowh=0; place={}
for n in sorted(names,key=lambda n:-sprites[n][0].shape[0]):
    arr=sprites[n][0]; hh,ww=arr.shape[:2]
    if x+ww>aw: x=0; y+=rowh+pad; rowh=0
    place[n]=(x,y,ww,hh); x+=ww+pad; rowh=max(rowh,hh)
atlas=np.zeros((y+rowh,aw,4),np.uint8)
for n,(x,y,ww,hh) in place.items(): atlas[y:y+hh,x:x+ww]=sprites[n][0]
Image.fromarray(atlas,'RGBA').save('app/sprites.png',optimize=True)
img.save('app/design.png',optimize=True)
cs=['// automatisch erzeugt von gen32.py aus dem Original-Design (1538x1023). Nicht von Hand ändern.',
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
