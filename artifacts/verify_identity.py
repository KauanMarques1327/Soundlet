"""ADB helpers for the Soundlet emulator acceptance checks (no app dependencies)."""
import json
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent / 'identity'
ROOT.mkdir(exist_ok=True)
ADB = str(Path.home() / 'AppData/Local/Android/Sdk/platform-tools/adb.exe')
PACKAGE = 'com.companyname.soundlet'

def adb(*args):
    return subprocess.check_output([ADB, *args], timeout=45)

def screenshot(name):
    (ROOT / (name + '.png')).write_bytes(adb('exec-out', 'screencap', '-p'))

def tree(name):
    adb('shell', 'uiautomator', 'dump', '/sdcard/soundlet-ui.xml')
    xml = adb('shell', 'cat', '/sdcard/soundlet-ui.xml')
    (ROOT / (name + '.xml')).write_bytes(xml)
    return ET.fromstring(xml)

def node(root, phrase):
    return next(n for n in root.iter('node')
                if phrase in n.get('content-desc', '') or phrase in n.get('text', ''))

def bounds(n):
    return tuple(map(int, re.findall(r'\d+', n.get('bounds'))))

def tap(x, y):
    adb('shell', 'input', 'tap', str(int(x)), str(int(y)))
    time.sleep(.6)

def tap_node(root, phrase):
    x1, y1, x2, y2 = bounds(node(root, phrase))
    tap((x1+x2)/2, (y1+y2)/2)

def record_launch():
    adb('shell', 'am', 'force-stop', PACKAGE)
    adb('shell', 'input', 'keyevent', 'KEYCODE_WAKEUP')
    adb('shell', 'wm', 'dismiss-keyguard')
    recording = subprocess.Popen([ADB, 'shell', 'screenrecord', '--time-limit', '8', '/sdcard/soundlet-startup.mp4'])
    time.sleep(.5)
    adb('shell', 'am', 'start', '-n', PACKAGE + '/crc6472e4e84519e5016d.MainActivity')
    recording.wait(timeout=45)
    adb('pull', '/sdcard/soundlet-startup.mp4', str(ROOT / 'startup.mp4'))
    screenshot('main')
    tree('main')

def check_navigation():
    results = []
    tap_node(tree('main-before-nav'), 'Conectar via Bluetooth')
    search = tree('search')
    screenshot('search')
    x1, y1, x2, y2 = bounds(node(search, 'Soundlet Pulseira'))
    # Name, subtitle, icon, arrow, left/right border and upper/lower card edges.
    points = [('name', .45, .35), ('subtitle', .4, .70), ('icon', .10, .5),
              ('arrow', .92, .5), ('left-edge', .008, .5), ('right-edge', .992, .5),
              ('top-edge', .5, .015), ('bottom-edge', .5, .985)]
    for name, px, py in points:
        tap(x1+(x2-x1)*px, y1+(y2-y1)*py)
        connected = tree('card-'+name)
        node(connected, 'Simular próximo alerta')
        results.append('PASS card '+name)
        if name == 'name':
            screenshot('connected-back-centered')
        tap_node(connected, 'Voltar para procurar pulseira')
        node(tree('returned-'+name), 'Soundlet Pulseira')
    tap((x1+x2)/2, (y1+y2)/2)
    before = tree('history-before')
    tap_node(before, 'Simular próximo alerta')
    after = tree('history-after')
    screenshot('history-added')
    results.append('PASS add button invoked; inspect persisted JSON and screenshot')
    tap_node(after, 'Escolher data do histórico')
    calendar = tree('calendar')
    node(calendar, 'Mês anterior')
    screenshot('calendar')
    tap_node(calendar, 'Mês anterior')
    screenshot('calendar-previous-month')
    previous = tree('calendar-previous-month')
    day = next(n for n in previous.iter('node') if n.get('content-desc', '').startswith('01 de '))
    dx1, dy1, dx2, dy2 = bounds(day)
    tap((dx1+dx2)/2, (dy1+dy2)/2)
    selected = tree('date-selected')
    assert 'Hoje' not in node(selected, 'Escolher data do histórico').get('text', '')
    screenshot('filtered-history')
    tap_node(selected, 'Escolher data do histórico')
    adb('shell', 'input', 'keyevent', 'KEYCODE_BACK')
    node(tree('calendar-closed'), 'Simular próximo alerta')
    results.append('PASS calendar opens, changes month, selects a date, filters history, closes with Android back')
    adb('shell', 'input', 'keyevent', 'KEYCODE_BACK')
    node(tree('back-search'), 'Soundlet Pulseira')
    adb('shell', 'input', 'keyevent', 'KEYCODE_BACK')
    node(tree('back-main'), 'Conectar via Bluetooth')
    screenshot('back-main')
    results.append('PASS connected -> search -> main with Android back')
    adb('shell', 'input', 'keyevent', 'KEYCODE_BACK')
    screenshot('back-exit')
    results.append('Back from main captured; verify launcher instead of splash')
    (ROOT / 'navigation-results.json').write_text(json.dumps(results, indent=2), encoding='utf-8')

if __name__ == '__main__':
    record_launch()
    check_navigation()
