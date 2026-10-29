# Python-Nachbau (keine Windows-Screenshots): 3.4 Titel/Untertitel immer aus dem Original-Bild
from compose33 import render, spr
from PIL import Image, ImageDraw, ImageFont, ImageChops
F=ImageFont.truetype('app/BarlowSemiCondensed-Medium.ttf',26)
orig=Image.open('app/design.png').convert('RGBA')
on=render({}); off=render({}); spr(off,'title_clean'); spr(off,'sub_clean')
def lab(im,t):
    c=Image.new('RGBA',(im.width,im.height+40),(18,18,20,255)); c.paste(im,(0,40)); ImageDraw.Draw(c).text((10,6),t,font=F,fill=(240,240,240)); return c
def diff(a,b): d=ImageChops.difference(a.convert('RGB'),b.convert('RGB')); return d.point(lambda v:min(255,v*8)).convert('RGBA')
T=(380,20,1160,250); S=(560,740,980,790)
def row(box,s,name):
    a,b=orig.crop(box),on.crop(box); z=lambda i:i.resize((i.width*s,i.height*s),Image.LANCZOS)
    m=max(ImageChops.difference(a.convert('RGB'),b.convert('RGB')).getextrema(),key=lambda e:e[1])[1]
    return [lab(z(a),f'Original {name} ({s}x)'),lab(z(b),f'3.4 gerendert ({s}x)'),lab(z(diff(a,b)),f'Differenz x8 (max {m})')]
rows=[row(T,1,'Titel'),row(S,2,'Untertitel')]
full=[lab(orig.resize((769,512)),'Original-Design 100%'),lab(on.resize((769,512)),'3.4 Leerlauf, Texte AN'),lab(off.resize((769,512)),'3.4 Texte oben+unten AUS')]
W=max(sum(c.width for c in r) for r in rows+[full])+40; H=sum(max(c.height for c in r) for r in rows+[full])+40
out=Image.new('RGBA',(W,H),(18,18,20,255)); y=0
for r in [full]+rows:
    x=0
    for c in r: out.paste(c,(x,y)); x+=c.width+20
    y+=max(c.height for c in r)+20
out.convert('RGB').save('/workspace/terminator-3.4-states.png'); print(out.size)
