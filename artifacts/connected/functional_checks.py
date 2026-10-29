from device_check import adb, dump, tap_text, shot, OUT, ADB
import datetime as dt
import json
import subprocess
import time

results = []
def check(condition, name):
    if not condition: raise AssertionError(name)
    results.append(name)
    print('PASS', name, flush=True)
    (OUT / 'device-results.json').write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding='utf-8')

def texts(name):
    return [n.get('text', '') for n in dump(name).iter('node')]

def stored():
    return json.loads(adb('shell', 'run-as', 'com.companyname.soundlet', 'cat', 'files/alert-history.json'))

records = stored()
check(len(records) == 6 and [r['Type'] for r in records] == list(range(6)), 'Six taps create the requested sequence')
check(len({r['Id'] for r in records}) == 6, 'All records have unique IDs')
root = dump('order-confirmed')
titles = [n.get('text') for n in root.iter('node') if n.get('text') in ['Alerta de incêndio', 'Batidas na porta', 'Alerta de sirene', 'Campainha tocando', 'Choro de bebê', 'Latido de cachorro']]
check(titles == ['Latido de cachorro','Choro de bebê','Campainha tocando','Alerta de sirene','Batidas na porta','Alerta de incêndio'], 'Visible history is newest first')

tap_text('Escolher data do histórico', True)
check('Outubro 2026' in texts('calendar-current'), 'Calendar opens at device current month')
tap_text('Próximo mês', True)
check('Novembro 2026' in texts('calendar-next'), 'Next month')
tap_text('Mês anterior', True)
check('Outubro 2026' in texts('calendar-previous'), 'Previous month')
tap_text('24 de outubro de 2026', True)
filtered = texts('date-filter-empty')
check(any('24/10/2026' in x for x in filtered) and 'Nenhum alerta registrado nesta data' in filtered and 'Latido de cachorro' not in filtered, 'Date selection filters history and shows empty state')
shot('filtered-empty')
tap_text('Escolher data do histórico', True)
tap_text('25 de outubro de 2026', True)
check('Latido de cachorro' in texts('today-filter-restored'), 'Selecting today restores records')
tap_text('Escolher data do histórico', True)
adb('shell', 'input', 'tap', '520', '600')
time.sleep(1)
check('Mês anterior' not in [n.get('content-desc') for n in dump('outside-close').iter('node')], 'Tap outside dismisses calendar')
tap_text('Escolher data do histórico', True)
tap_text('Escolher data do histórico', True)
check('Mês anterior' not in [n.get('content-desc') for n in dump('toggle-close').iter('node')], 'Second tap toggles calendar closed')
tap_text('Escolher data do histórico', True)
adb('shell', 'input', 'keyevent', '4')
time.sleep(1)
check('Pulseira conectada' in texts('android-back-calendar') and 'Mês anterior' not in [n.get('content-desc') for n in dump().iter('node')], 'Android Back closes calendar and retains page')

before = int(adb('shell', 'date', '+%s').strip())
recorder = subprocess.Popen([ADB, '-s', 'emulator-5554', 'shell', 'screenrecord', '--time-limit', '8', '/sdcard/soundlet-arrival.mp4'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
time.sleep(1)
tap_text('Simular próximo alerta', True)
time.sleep(1)
records = stored()
after = int(adb('shell', 'date', '+%s').strip())
check(len(records) == 7 and records[-1]['Type'] == 0, 'Seventh tap wraps to fire without replacing records')
when = dt.datetime.fromisoformat(records[-1]['OccurredAt'])
check(before <= when.timestamp() <= after, 'Occurrence timestamp matches actual device tap time')
visible = texts('seventh-alert')
check(when.strftime('%H:%M') in visible, 'Displayed time uses Brazilian HH:mm')
shot('connected-history-final')
recorder.wait(timeout=30)
adb('pull', '/sdcard/soundlet-arrival.mp4', str(OUT / 'arrival.mp4'))

adb('shell','input','swipe','530','1950','530','1250','350')
time.sleep(1)
shot('history-scrolled')
check('Alerta de incêndio' in texts('scrolled-ui'), 'History remains scrollable')
tap_text('Voltar para procurar pulseira', True)
check(any('Voltar para a tela inicial' in n.get('content-desc','') for n in dump('back-bluetooth').iter('node')), 'Connected back returns to Bluetooth')
tap_text('Voltar para a tela inicial', True)
check(any('Conectar via Bluetooth' in n.get('content-desc','') for n in dump('back-main').iter('node')), 'Existing Bluetooth back returns to Main')
print('Device checks complete', flush=True)
