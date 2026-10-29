# Nachbau der Zeichenlogik von Terminator 3.2 (gleiche Assets, gleiche Koordinaten) in Quellauflösung 1538x1023.
from PIL import Image, ImageDraw, ImageFont
import re, math
cs=open('app/Sprites.cs').read()
atlas=Image.open('app/sprites.png').convert('RGBA')
AT={m[0]:tuple(map(int,m[1:])) for m in re.findall(r'\{ "(\w+)", new Rectangle\((\d+), (\d+), (\d+), (\d+)\) \}',cs)}
POS={m[0]:(int(m[1]),int(m[2])) for m in re.findall(r'\{ "(\w+)", new Point\((\d+), (\d+)\) \}',cs)}
DX=list(map(int,re.search(r'DigitX = \{ ([^}]*) \}',cs).group(1).split(','))); DY=int(re.search(r'DigitY = (\d+)',cs).group(1))
DX0=int(re.search(r'DigitX0 = (\d+)',cs).group(1)); CX=list(map(int,re.search(r'ColonX = \{ ([^}]*) \}',cs).group(1).split(',')))
SEG={'0':'abcdef','1':'bc','2':'abged','3':'abgcd','4':'fgbc','5':'afgcd','6':'afgedc','7':'abc','8':'abcdefg','9':'abcdfg'}
def spr(im,n,dx=0,dy=0):
    x,y,w,h=AT[n]; im.alpha_composite(atlas.crop((x,y,x+w,y+h)),(POS[n][0]+dx,POS[n][1]+dy))
def render(state):
    im=Image.open('app/design.png').convert('RGBA')
    # Bedienleiste: Zustände als Sprites
    q=state.get('quick','30')
    for n in ('q15','q30','q60','q120'): spr(im, n+('_on' if n[1:]==q else '_off'))
    spr(im,'in_on' if state.get('in',True) else 'in_off'); spr(im,'at_on' if not state.get('in',True) else 'at_off')
    spr(im,'unit_std' if state.get('unit')=='STD' else 'unit_off')
    digits=state.get('digits','003000')
    if digits!='003000' or state.get('running'):
        spr(im,'panel')
        for i,ch in enumerate(digits):
            for s in SEG[ch]: spr(im,'seg_'+s, DX[i]-DX0, 0)
        for cx in CX: x,y,w,h=AT['colon']; im.alpha_composite(atlas.crop((x,y,x+w,y+h)),(cx,POS['colon'][1]))
    return im
if __name__=='__main__':
    render({'digits':'128947','quick':'15','in':False,'unit':'STD'}).crop((180,520,1400,1000)).save('/tmp/zz_c32a.png')
