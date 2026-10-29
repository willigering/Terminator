# Python-NACHBAU der Zeichenlogik von Terminator 3.6 (gleiche Assets/Koordinaten/Formeln wie Program.cs/Art.cs),
# gerendert in Designauflösung 1538x1023. Kein Windows-Screenshot.
from PIL import Image, ImageDraw, ImageFont
import re
K=1537/960; SS=4
cs=open('app/Sprites.cs').read(); atlas=Image.open('app/sprites.png').convert('RGBA')
AT={m[0]:tuple(map(int,m[1:])) for m in re.findall(r'\{ "(\w+)", new Rectangle\((\d+), (\d+), (\d+), (\d+)\) \}',cs)}
POS={m[0]:(int(m[1]),int(m[2])) for m in re.findall(r'\{ "(\w+)", new Point\((\d+), (\d+)\) \}',cs)}
R={m[0]:tuple(map(float,m[1:])) for m in re.findall(r'R_(\w+) = new RectangleF\(([\d.]+)f?, ([\d.]+)f?, ([\d.]+)f?, ([\d.]+)f?\)',cs)}
MED='app/BarlowSemiCondensed-Medium.ttf'
SEG={'0':'abcdef','1':'bc','2':'abged','3':'abgcd','4':'fgbc','5':'afgcd','6':'afgedc','7':'abc','8':'abcdefg','9':'abcdfg'}
def spr(im,n):
    x,y,w,h=AT[n]; im.alpha_composite(atlas.crop((x,y,x+w,y+h)),POS[n])
class V:  # Vektor-Ebene in logischen Pixeln, SSx überabgetastet
    def __init__(s,W,H): s.im=Image.new('RGBA',(int(W*K*SS),int(H*K*SS)),(0,0,0,0)); s.d=ImageDraw.Draw(s.im)
    def P(s,x,y): return (x*K*SS,y*K*SS)
    def font(s,px): return ImageFont.truetype(MED,int(round(px*K*SS)))
    def width(s,t,px): return s.font(px).getlength(t)/(K*SS)
    def text(s,t,px,col,x,cy,align):
        f=s.font(px); w=s.width(t,px); asc=f.getmetrics()[0]/(K*SS); cap=px*0.70
        ox={-1:x,0:x-w/2,1:x-w}[align]
        s.d.text(s.P(ox,cy+cap/2-asc),t,font=f,fill=col)
    def poly(s,pts,col): s.d.polygon([s.P(*p) for p in pts],fill=col)
    def done(s,im,blur=0):
        o=s.im.resize(im.size,Image.LANCZOS); im.alpha_composite(o)
GREY=(225,226,229); RED=(252,244,240); WHITE=(253,254,254); SR=(238,72,62); SG=(226,226,228)
TP,SP,STP,R1,R2=11.5,15.2,13.0,561.2,596.8
def digits(im,t):
    from PIL import ImageFilter
    spr(im,'panel')
    PC=[418.2,477.05,536.65]; CC=[447.8,506.5]; CY=360.1; H=18.6; W=10.2; T=2.6; P=15.6; SL=0.12; G=0.4
    def hz(x1,x2,cy): return [(x1,cy),(x1+T/2,cy-T/2),(x2-T/2,cy-T/2),(x2,cy),(x2-T/2,cy+T/2),(x1+T/2,cy+T/2)]
    def vt(cx,y1,y2): return [(cx,y1),(cx+T/2,y1+T/2),(cx+T/2,y2-T/2),(cx,y2),(cx-T/2,y2-T/2),(cx-T/2,y1+T/2)]
    polys=[]
    for i,ch in enumerate(t):
        cx=PC[i//2]+(-P/2 if i%2==0 else P/2); x=cx-W/2; y=CY-H/2; m=y+H/2; mid=y+H/2
        g={'a':hz(x+G,x+W-G,y),'g':hz(x+G,x+W-G,m),'d':hz(x+G,x+W-G,y+H),'f':vt(x,y+G,m-G),'e':vt(x,m+G,y+H-G),'b':vt(x+W,y+G,m-G),'c':vt(x+W,m+G,y+H-G)}
        for s_ in SEG[ch]: polys.append([(px-SL*(py-mid),py) for px,py in g[s_]])
    glow=V(960,639); core=V(960,639); body=V(960,639)
    for p in polys:
        glow.poly(p,(255,60,30,255)); body.poly(p,(255,88,62,255))
        c=(sum(a for a,_ in p)/6,sum(b for _,b in p)/6); core.poly([(c[0]+(a-c[0])*.55,c[1]+(b-c[1])*.45) for a,b in p],(255,150,120,170))
    for cx in CC:
        for dy in (-4.2,4.2):
            e=[glow.P(cx-1.35,CY+dy-1.35),glow.P(cx+1.35,CY+dy+1.35)]
            glow.d.ellipse(e,fill=(255,60,30,255)); body.d.ellipse(e,fill=(255,88,62,255))
    gl=glow.im.resize(im.size,Image.LANCZOS).filter(ImageFilter.GaussianBlur(2.6*K))
    a=gl.split()[3].point(lambda v:int(v*0.55)); gl.putalpha(a); im.alpha_composite(gl)
    body.done(im); core.done(im)
def render(st):
    im=Image.open('app/design.png').convert('RGBA'); v=V(960,639)
    q=st.get('quick','30'); inn=st.get('in',True)
    def btn(id_,lab,on,cx=None):
        spr(im,id_+('_bg_on' if on else '_bg_off')); r=R[id_]
        v.text(lab,TP,RED if on else GREY,cx or r[0]+r[2]/2,R1,0)
    btn('in','IN',inn,151.6)
    for n,l in (('q15','15 MIN'),('q30','30 MIN'),('q60','1 STD'),('q120','2 STD')): btn(n,l,inn and n[1:]==q)
    btn('at','UM',not inn); btn('unit',st.get('unit','MIN'),False)
    spr(im,'num_clean'); v.text(str(st.get('num',30)),TP,WHITE,208.0,R1,0)
    spr(im,'time_clean'); hh,mm=st.get('time','23:00').split(':'); cw=v.width(':',TP)
    v.text(hh,TP,WHITE,683.9-cw/2-0.3,R1,1); v.text(':',TP,WHITE,683.9,R1-0.3,0); v.text(mm,TP,WHITE,683.9+cw/2+0.3,R1,-1)
    spr(im,'act_clean'); v.text(st.get('action','Herunterfahren'),TP,WHITE,134.9,R2,-1)
    spr(im,'start_clean'); tw=v.width('START',SP); x0=484.5-(7.8+14+tw)/2
    v.poly([(x0,R2-5),(x0+7.8,R2),(x0,R2+5)],WHITE); v.text('START',SP,WHITE,x0+21.8,R2,-1)
    spr(im,'cancel_clean'); v.text('ABBRECHEN',TP,GREY,711.7,R2,0)
    spr(im,'status_clean'); s=st.get('status','BEREIT – Zeit und Aktion wählen, dann START')
    cut=s.find(' – '); h,t=(s[:cut],s[cut:]) if cut>0 else (s,'')
    w1=v.width(h,STP); w2=v.width(t,STP) if t else 0; x=475.0-(w1+w2)/2
    v.text(h,STP,SR,x,530.9,-1)
    if t: v.text(t,STP,SG,x+w1,530.9,-1)
    v.done(im); digits(im,st.get('digits','003000'))
    return im
