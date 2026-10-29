import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
sys.stdout.reconfigure(encoding='utf-8')

ADB = str(Path(os.environ['LOCALAPPDATA']) / 'Android/Sdk/platform-tools/adb.exe')
OUT = Path(__file__).resolve().parent

def adb(*args):
    return subprocess.check_output([ADB, '-s', 'emulator-5554', *args], timeout=45)

def shot(name):
    (OUT / (name + '.png')).write_bytes(adb('exec-out', 'screencap', '-p'))

def dump(name='ui'):
    for attempt in range(4):
        result = adb('shell', 'uiautomator', 'dump', '/sdcard/soundlet-ui.xml')
        if b'dumped to' in result:
            data = adb('exec-out', 'cat', '/sdcard/soundlet-ui.xml')
            (OUT / (name + '.xml')).write_bytes(data)
            return ET.fromstring(data)
        time.sleep(1)
    raise AssertionError('Android accessibility tree unavailable')

def tap_text(text, desc=False):
    root = dump()
    for node in root.iter('node'):
        if text in node.get('content-desc' if desc else 'text', ''):
            x1, y1, x2, y2 = map(int, re.findall(r'\d+', node.get('bounds')))
            adb('shell', 'input', 'tap', str((x1+x2)//2), str((y1+y2)//2))
            time.sleep(1)
            return
    raise AssertionError('Missing control: ' + text)

if __name__ == '__main__':
    if sys.argv[1] == 'shot': shot(sys.argv[2])
    elif sys.argv[1] == 'dump':
        for n in dump(sys.argv[2]).iter('node'):
            if n.get('text') or n.get('content-desc'):
                print(n.get('text'), n.get('content-desc'), n.get('bounds'))
    elif sys.argv[1] == 'tap': tap_text(sys.argv[2], len(sys.argv) > 3)
