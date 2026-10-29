"""Read-only comparison of emulator frames: moving arcs and stationary symbol."""
from PIL import Image, ImageChops
from pathlib import Path
import xml.etree.ElementTree as ET
import re, json
out=Path(__file__).resolve().parent
results=[]
for base,xml_name,central in [
 ('03-wifi-search','03-wifi-search',(298,434,264,264)),
 ('08-wifi-resumed','wifi-return-right-edge',(298,434,264,264)),
 ('08-wifi-window-resumed','08-wifi-window-resumed',(298,434,264,264)),
 ('11-bluetooth-search','11-bluetooth-search',(288,421,274,274))]:
 a=Image.open(out/(base+'-a.png')).convert('RGB')
 b=Image.open(out/(base+'-b.png')).convert('RGB')
 viewport=next(n for n in ET.parse(out/(xml_name+'.xml')).iter('node') if n.get('class')=='android.widget.ImageView')
 x1,y1,x2,y2=map(int,re.findall(r'\d+',viewport.get('bounds')))
 scale=max((x2-x1)/850,(y2-y1)/1850)
 ox=x1+((x2-x1)-850*scale)/2
 oy=y1+((y2-y1)-1850*scale)/2
 diff=ImageChops.difference(a,b)
 def changed(rect):
  x,y,w,h=rect
  box=tuple(round(v) for v in (ox+x*scale,oy+y*scale,ox+(x+w)*scale,oy+(y+h)*scale))
  return sum(p!=(0,0,0) for p in diff.crop(box).getdata())
 central_count=changed(central)
 small_count=changed((425,933.5,30,30))
 cx=ox+(central[0]+central[2]/2)*scale
 cy=oy+(central[1]+central[3]/2)*scale
 radius=100*scale
 pixels=diff.load()
 stationary_changes=sum(pixels[x,y]!=(0,0,0) for x in range(round(cx-radius),round(cx+radius))
  for y in range(round(cy-radius),round(cy+radius)) if (x-cx)**2+(y-cy)**2<radius**2)
 result=dict(screen=base,central_arc_changed_pixels=central_count,small_arc_changed_pixels=small_count,
             stationary_symbol_changed_pixels=stationary_changes)
 results.append(result)
 print(result)
 assert central_count>0 and small_count>0 and stationary_changes==0, result
(out/'animation-results.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
