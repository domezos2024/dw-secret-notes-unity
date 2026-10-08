import os, re, shutil, sys, json, xml.etree.ElementTree as ET
here = os.path.dirname(os.path.abspath(__file__))
unity = os.path.abspath(os.path.join(here, '..'))
res = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else next((p for p in (os.path.join(unity, '..', 'app', 'src', 'main', 'res'), os.path.join(unity, '..', 'dw-secret-notes', 'app', 'src', 'main', 'res')) if os.path.isdir(p)), ''))
i18n = os.path.join(unity, 'Assets', 'DwSecretNotes', 'Resources', 'i18n')
lib = os.path.join(unity, 'Assets', 'Plugins', 'Android', 'DwNative.androidlib', 'res')
tags = {'values': 'en', 'values-zh-rCN': 'zh-CN'}
def clean(s):
    s = (s or '').replace("\\'", "'").replace('\\"', '"').replace('\\n', '\n')
    s = re.sub(r'%(\d)\$[sd]', lambda g: '{%d}' % (int(g.group(1)) - 1), s)
    return s.replace('%d', '{0}').replace('%s', '{0}')
def esc(t): return t.replace('&', '&amp;').replace('<', '&lt;')
os.makedirs(i18n, exist_ok=True)
for d in sorted(os.listdir(res)):
    f = os.path.join(res, d, 'strings.xml')
    if not d.startswith('values') or not os.path.isfile(f): continue
    root = ET.parse(f).getroot(); keys, vals, widget = [], [], []
    for e in root:
        n = e.get('name')
        if e.tag == 'string':
            keys.append(n); vals.append(clean(''.join(e.itertext())))
            if n in ('widget_description', 'widget_launcher_description', 'app_name'):
                widget.append('    <string name="dw_%s">%s</string>' % ('widget_app_name' if n == 'app_name' else n, esc(''.join(e.itertext()))))
        elif e.tag == 'plurals':
            for it in e: keys.append(n + '_' + it.get('quantity')); vals.append(clean(''.join(it.itertext())))
    tag = tags.get(d, d[7:])
    with open(os.path.join(i18n, tag + '.json'), 'w', encoding='utf-8') as o: json.dump({'keys': keys, 'values': vals}, o, ensure_ascii=False)
    os.makedirs(os.path.join(lib, d), exist_ok=True)
    with open(os.path.join(lib, d, 'strings.xml'), 'w', encoding='utf-8') as o: o.write('<?xml version="1.0" encoding="utf-8"?>\n<resources>\n' + '\n'.join(widget) + '\n</resources>\n')
    print('strings', tag, len(keys))
for sub in ('layout', 'drawable', 'xml'): os.makedirs(os.path.join(lib, sub), exist_ok=True)
for n in ('widget_background.xml', 'widget_button_background.xml', 'widget_field_background.xml'): shutil.copy(os.path.join(res, 'drawable', n), os.path.join(lib, 'drawable', n))
icon = os.path.join(res, 'drawable', 'app_icon.png')
shutil.copy(icon, os.path.join(lib, 'drawable', 'dw_widget_icon.png'))
for dst in (('Assets', 'DwSecretNotes', 'Resources', 'UI'), ('Assets', 'DwSecretNotes', 'Branding')):
    p = os.path.join(unity, *dst); os.makedirs(p, exist_ok=True); shutil.copy(icon, os.path.join(p, 'app_icon.png'))
def rewrite(src, dst, repl):
    t = open(src, encoding='utf-8').read()
    for a, b in repl: t = t.replace(a, b)
    open(dst, 'w', encoding='utf-8').write(t)
rewrite(os.path.join(res, 'layout', 'widget_secret_encrypt.xml'), os.path.join(lib, 'layout', 'dw_widget_secret.xml'), [])
rewrite(os.path.join(res, 'layout', 'widget_launcher.xml'), os.path.join(lib, 'layout', 'dw_widget_launcher.xml'), [('@drawable/app_icon', '@drawable/dw_widget_icon'), ('@string/app_name', '@string/dw_widget_app_name')])
rewrite(os.path.join(res, 'xml', 'secret_widget_info.xml'), os.path.join(lib, 'xml', 'dw_secret_widget_info.xml'), [('@drawable/app_icon', '@drawable/dw_widget_icon'), ('@layout/widget_secret_encrypt', '@layout/dw_widget_secret'), ('@string/widget_description', '@string/dw_widget_description')])
rewrite(os.path.join(res, 'xml', 'launcher_widget_info.xml'), os.path.join(lib, 'xml', 'dw_launcher_widget_info.xml'), [('@drawable/app_icon', '@drawable/dw_widget_icon'), ('@layout/widget_launcher', '@layout/dw_widget_launcher'), ('@string/widget_launcher_description', '@string/dw_widget_launcher_description')])
print('done ->', unity)
