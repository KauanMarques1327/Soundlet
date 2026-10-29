from pathlib import Path

out = Path(__file__).resolve().parents[2] / 'Soundlet/Resources/Images'
background = '''<svg xmlns="http://www.w3.org/2000/svg" width="735" height="1600" viewBox="0 0 735 1600">
<defs><radialGradient id="bg" cx="0" cy="0.25" r="1"><stop stop-color="#002643"/><stop offset=".53" stop-color="#000e1b"/><stop offset="1" stop-color="#00070f"/></radialGradient>
<linearGradient id="blue" x2="1" y2="1"><stop stop-color="#064e99"/><stop offset="1" stop-color="#00162c"/></linearGradient></defs>
<path fill="url(#bg)" d="M0 0H735V1600H0Z"/>
<path fill="url(#blue)" stroke="#1465ac" stroke-width="2" d="M0 205C85 85 269 164 294 0H0Z"/>
<path fill="url(#blue)" stroke="#157ac5" stroke-width="2" d="M439 1600C498 1481 653 1546 735 1430V1600Z"/>
<g fill="none" stroke="#fff43b"><path opacity=".025" stroke-width="35" d="M335 0C313 211 101 145 37 319L0 411 M346 1600C415 1406 618 1496 735 1352"/>
<path opacity=".06" stroke-width="18" d="M335 0C313 211 101 145 37 319L0 411 M346 1600C415 1406 618 1496 735 1352"/>
<path opacity=".16" stroke-width="7" d="M335 0C313 211 101 145 37 319L0 411 M346 1600C415 1406 618 1496 735 1352"/>
<path stroke-width="2.5" d="M335 0C313 211 101 145 37 319L0 411 M346 1600C415 1406 618 1496 735 1352"/></g></svg>'''
(out / 'connected_background.svg').write_text(background)
watch = '''<svg xmlns="http://www.w3.org/2000/svg" width="250" height="250" viewBox="0 0 250 250">
<defs><linearGradient id="cyan" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#aaefff"/><stop offset="1" stop-color="#00cde5"/></linearGradient></defs>
<circle cx="125" cy="125" r="120" fill="none" stroke="#06304c" stroke-width="2"/>
<circle cx="125" cy="125" r="104" fill="none" stroke="#00bded" opacity=".07" stroke-width="18"/>
<path d="M159 25A105 105 0 1 1 27 89" fill="none" stroke="#00c9ef" stroke-width="9" stroke-linecap="round"/>
<circle cx="125" cy="125" r="82" fill="none" stroke="#05749d" stroke-width="2"/>
<g stroke="#00d5f5" fill="none"><rect x="91" y="92" width="58" height="66" rx="11" opacity=".09" stroke-width="22"/>
<rect x="91" y="92" width="58" height="66" rx="11" opacity=".14" stroke-width="14"/></g>
<path d="M100 95L104 78Q104 76 110 76H131Q137 76 137 82L140 96M100 155L104 173Q104 176 110 176H131Q137 176 137 170L140 155" fill="url(#cyan)"/>
<rect x="93" y="95" width="54" height="60" rx="10" fill="#001322" stroke="url(#cyan)" stroke-width="7"/>
<path d="M151 112V127" stroke="#00d7ee" stroke-width="5" stroke-linecap="round"/>
<circle cx="174" cy="166" r="27" fill="#00e5a1" opacity=".12"/><circle cx="174" cy="166" r="24" fill="#00dca0"/>
<path d="M163 165L172 174L186 158" fill="none" stroke="#96ffe0" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></svg>'''
(out / 'connected_bracelet.svg').write_text(watch)
icons = {
 'fire': ('#ff6b79', 'M15 27C4 22 11 15 14 10L15 17C20 12 20 5 19 3C30 14 32 24 23 28M17 29C12 26 18 20 20 18C19 23 25 25 21 29'),
 'door': ('#24d6ff', 'M7 4H24V28H7ZM9 6L20 8V26L9 28ZM16 17H17M27 8H29V25H27'),
 'siren': ('#ff7e91', 'M6 27H27M8 24H25V27H8ZM10 24V14C10 5 23 5 23 14V24M16 2V0M5 8L2 5M27 8L30 5'),
 'bell': ('#ffe563', 'M5 23C10 18 8 11 11 8C14 4 22 5 24 10C26 14 23 18 28 23ZM13 27C14 31 20 31 21 27M16 5V2'),
 'baby': ('#d5b4ff', 'M7 13C1 10 0 21 6 22M26 13C32 10 33 21 27 22M27 18C27 33 5 33 5 18C5 9 11 9 15 5C17 0 21 9 17 11M12 18H12.2M21 18H21.2M12 24Q17 28 22 23'),
 'dog': ('#00d7f4', 'M8 12L3 8L7 2L12 8H22L26 2L30 8L25 12L27 28H20L18 21H14L12 28H5ZM10 12V17M23 12V17M12 28H20')}
for name, (color, path) in icons.items():
    (out / f'alert_{name}.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="-3 -3 38 38"><path d="{path}" fill="none" stroke="{color}" stroke-width="5" opacity=".14" stroke-linecap="round" stroke-linejoin="round"/><path d="{path}" fill="none" stroke="{color}" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/></svg>')
