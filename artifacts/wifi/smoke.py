"""ADB smoke checks for Soundlet. Preserves app data; adds one simulated alert per flow."""
import subprocess, pathlib, time, re, json, datetime, xml.etree.ElementTree as ET
ADB = r'C:\Users\kauan\AppData\Local\Android\sdk\platform-tools\adb.exe'
OUT = pathlib.Path(__file__).resolve().parent
results = []

def adb(*args):
    return subprocess.check_output([ADB, *args], timeout=60, stderr=subprocess.STDOUT).decode('utf-8', errors='replace')

def dump(name):
    for attempt in range(4):
        output=adb('shell', 'uiautomator', 'dump', '/sdcard/soundlet-ui.xml')
        if 'dumped to:' in output: break
        time.sleep(2)
    else: raise RuntimeError('UI dump failed: '+output)
    adb('pull', '/sdcard/soundlet-ui.xml', str(OUT / (name + '.xml')))
    return ET.parse(OUT / (name + '.xml')).getroot()

def node(root, text, attr='content-desc'):
    return next(n for n in root.iter('node') if n.get(attr) == text)

def bounds(n):
    return tuple(map(int, re.findall(r'\d+', n.get('bounds'))))

def tap_node(n):
    x1,y1,x2,y2 = bounds(n)
    tap((x1+x2)//2, (y1+y2)//2)

def tap(x,y):
    adb('shell','input','tap',str(x),str(y)); time.sleep(2)

def shot(name):
    adb('shell','screencap','-p','/sdcard/soundlet-screen.png')
    adb('pull','/sdcard/soundlet-screen.png',str(OUT/(name+'.png')))

def check(name, test):
    assert test, name
    results.append({'check':name,'passed':True})
    (OUT/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    print('PASS:', name, flush=True)

def history():
    try: return json.loads(adb('shell','run-as','com.companyname.soundlet','cat','files/alert-history.json'))
    except subprocess.CalledProcessError: return []

def back(root):
    tap_node(node(root,'Voltar para procurar pulseira'))

root=dump('01-home-initial'); shot('01-home-initial')
check('Bluetooth selected on startup',node(root,'Conectar via Bluetooth, selecionado') is not None)
tap_node(node(root,'Conectar via Wi-Fi, não selecionado'))
root=dump('02-wifi-selected'); shot('02-wifi-selected')
check('First Wi-Fi tap only selects',node(root,'Conectar via Wi-Fi, selecionado') is not None)
time.sleep(3)
tap_node(node(root,'Conectar via Wi-Fi, selecionado'))
root=dump('03-wifi-search'); shot('03-wifi-search-a'); time.sleep(.23); shot('03-wifi-search-b')
card=node(root,'Soundlet Pulseira, ESP32-S3. Abrir pulseira conectada, conexão simulada')
check('Second normal Wi-Fi tap navigates',card is not None)
# All visual regions of the card: icon, name, subtitle, empty area, arrow and border margins.
x1,y1,x2,y2=bounds(card)
for name,fx,fy in [('icon',.10,.5),('name',.4,.30),('subtitle',.4,.7),('empty',.74,.75),('arrow',.93,.5),('left-edge',.01,.5),('right-edge',.99,.5)]:
    tap(int(x1+(x2-x1)*fx), int(y1+(y2-y1)*fy))
    root=dump('wifi-card-'+name)
    check('Wi-Fi card clickable: '+name,node(root,'Conectado via Wi-Fi','text') is not None)
    if name=='icon':
        shot('04-wifi-connected')
        previous=len(history())
        tap_node(node(root,'Simular próximo alerta')); root=dump('05-wifi-alert'); shot('05-wifi-alert')
        records=history()
        check('Wi-Fi simulator persists a new alert',len(records)==previous+1)
        device_time=datetime.datetime.fromisoformat(adb('shell','date','+%Y-%m-%dT%H:%M:%S').strip())
        alert_time=datetime.datetime.fromisoformat(records[-1]['OccurredAt']).replace(tzinfo=None)
        check('Alert uses actual device date and time',0 <= (device_time-alert_time).total_seconds() < 90)
        check('Simulation adds timestamped alert via Wi-Fi',any(re.fullmatch(r'\d{2}:\d{2}',n.get('text','')) for n in root.iter('node')))
        tap_node(node(root,'Escolher data do histórico')); root=dump('06-wifi-calendar'); shot('06-wifi-calendar')
        check('Calendar opens via Wi-Fi',node(root,'Mês anterior') is not None)
        tap_node(node(root,'1','text')); root=dump('06-filtered-date')
        check('Calendar changes history date', 'Hoje' not in node(root,'Escolher data do histórico').get('text',''))
        tap_node(node(root,'Escolher data do histórico')); root=dump('06-select-today')
        today=str(int(adb('shell','date','+%d').strip()))
        tap_node(node(root,today,'text')); root=dump('06-today-restored')
        check('Calendar restores today and its alerts', 'Hoje' in node(root,'Escolher data do histórico').get('text',''))
        tap_node(node(root,'Escolher data do histórico')); root=dump('06-calendar-reopened')
        # Android back closes the calendar before navigating.
        adb('shell','input','keyevent','4'); time.sleep(1); root=dump('07-calendar-closed')
        check('Back closes calendar and stays connected',node(root,'Conectado via Wi-Fi','text') is not None)
    back(root); root=dump('wifi-return-'+name)
    check('Connected returns to Wi-Fi: '+name,any(n.get('content-desc','').startswith('Soundlet Pulseira,') for n in root.iter('node')))
shot('08-wifi-resumed-a'); time.sleep(.23); shot('08-wifi-resumed-b')
adb('shell','input','keyevent','3'); time.sleep(2)
adb('shell','am','start','-n','com.companyname.soundlet/crc6472e4e84519e5016d.MainActivity'); time.sleep(3)
root=dump('08-wifi-window-resumed'); shot('08-wifi-window-resumed-a'); time.sleep(.23); shot('08-wifi-window-resumed-b')
check('Wi-Fi search survives background and resume', node(root,'Voltar para a tela inicial') is not None)
tap_node(node(root,'Voltar para a tela inicial')); root=dump('09-home-return')
check('Wi-Fi selection preserved after back',node(root,'Conectar via Wi-Fi, selecionado') is not None)
tap_node(node(root,'Conectar via Bluetooth, não selecionado')); root=dump('10-bluetooth-selected'); shot('10-bluetooth-selected')
check('First Bluetooth tap only selects',node(root,'Conectar via Bluetooth, selecionado') is not None)
tap_node(node(root,'Conectar via Bluetooth, selecionado')); root=dump('11-bluetooth-search'); shot('11-bluetooth-search-a'); time.sleep(.23); shot('11-bluetooth-search-b')
check('Second Bluetooth tap navigates',node(root,'Voltar para a tela inicial') is not None)
tap_node(node(root,'Soundlet Pulseira, ESP32-S3. Abrir pulseira conectada, conexão simulada')); root=dump('12-bluetooth-connected')
check('Existing connected page via Bluetooth',node(root,'Conectado via Bluetooth','text') is not None)
check('Wi-Fi history shared with Bluetooth',any(re.fullmatch(r'\d{2}:\d{2}',n.get('text','')) for n in root.iter('node')))
previous=len(history())
tap_node(node(root,'Simular próximo alerta')); root=dump('13-bluetooth-alert'); shot('13-bluetooth-alert')
check('Bluetooth simulator persists a new alert',len(history())==previous+1)
check('Simulation works via Bluetooth',any(re.fullmatch(r'\d{2}:\d{2}',n.get('text','')) for n in root.iter('node')))
tap_node(node(root,'Escolher data do histórico')); root=dump('14-bluetooth-calendar'); shot('14-bluetooth-calendar')
check('Calendar works via Bluetooth',node(root,'Mês anterior') is not None)
adb('shell','input','keyevent','4'); time.sleep(1)
adb('shell','input','keyevent','4'); time.sleep(1); root=dump('15-bluetooth-return')
check('Android back returns to Bluetooth search',node(root,'Voltar para a tela inicial') is not None)
tap_node(node(root,'Voltar para a tela inicial')); root=dump('16-home-final')
check('Bluetooth selection preserved after back',node(root,'Conectar via Bluetooth, selecionado') is not None)
print('All smoke checks passed.',flush=True)
