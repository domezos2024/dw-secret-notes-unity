# dw Secret Notes – Unity Edition

Unity-6-Neuauflage der Android-App `dw-secret-notes` (gleiche Funktionen, gleiches Backend, neue UI-Toolkit-Oberfläche).

- Unity: 6000.3.25f1, UI Toolkit (komplett aus C# aufgebaut), Built-in Render Pipeline
- Paket: `com.snote.domezos`, Version 6.0.0 (versionCode 47) – Update-Pfad der bestehenden Play-Store-App, sofern mit demselben Schlüssel signiert
- Android-Quellprojekt: `E:\AndroidStudioProjects\dw-secret-notes`

## Funktionen (1:1 aus der Android-App)

| Funktion | Umsetzung |
|---|---|
| Text + optionales Bild verschlüsseln, Link teilen/kopieren | `Net/NoteApi.cs`, `Crypto/*` – AES-256-GCM, PBKDF2-SHA256 (100 000 Iterationen), kompatibel zu WebApp/Android |
| Link/Alias entschlüsseln, 60-s-Countdown, Einmal-Lesen (unlink) | `UI/Screens/MainPage.cs` |
| Kurzlinks/Redirects auflösen | `NoteApi.ResolveLink` |
| Deep Links `https://domezos-ware.com/msges/view.php?...`, Teilen-Intent (Text) | `Plugins/Android/DwActivity.java`, `DwBridge.java`, `Core/LinkParser.cs` |
| 15 Sprachen inkl. Plural-Regeln, RTL (ar, ur) | `Core/Localization.cs`, `Resources/i18n/*.json` |
| 17 Themes (gleiche Farbableitung wie Compose) | `Core/ThemeCatalog.cs` |
| Hilfe, Info, TinyURL, Sprache, Theme-Auswahl | `UI/Screens/*`, `UI/Shell/AppShell.cs` |
| Homescreen-Widgets (Schnell-Verschlüsseln, Starter) | `Plugins/Android/DwNative.androidlib` |
| Bewertungsdialog (3. Start), In-App-Review, Sterne im Footer | `MainPage.OnShow`, `ReviewHelper.java` |
| FLAG_SECURE in Release-Builds | `DwBridge.setSecure` |

Neu in der Oberfläche: animierter Aurora-Hintergrund, Glas-Karten, Tabs Verschlüsseln/Entschlüsseln, Vektor-Icons, Countdown-Ring, Einfügen aus Zwischenablage, Bildvorschau mit Vollbild, Bottom-Sheets für Menü/Themes, Akkordeon-FAQ, Toasts.

Unterschied: Das Widget öffnet die App mit einem Schnell-Verschlüsseln-Sheet statt eines transparenten Overlays über dem Homescreen.

## Struktur

```
Assets/DwSecretNotes/Scripts/Core      Logging, Konfiguration, Prefs, L10n, Themes, Link-Parser
Assets/DwSecretNotes/Scripts/Crypto    PBKDF2-SHA256, AES-256-GCM, Payload
Assets/DwSecretNotes/Scripts/Net       Backend-API (msg_store.php)
Assets/DwSecretNotes/Scripts/Platform  Native Bridge (Android / Desktop)
Assets/DwSecretNotes/Scripts/UI        Shell, Komponenten, Seiten
Assets/DwSecretNotes/Editor            Projekt-Setup, Builds, Selbsttests
Assets/Plugins/Android                 Activity, Bridge, Widgets, Bildauswahl
Tools/sync_android_resources.py        übernimmt Strings/Widget-Ressourcen aus dem Android-Projekt
```

## Befehle

```powershell
$u = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe'
$p = 'E:\AndroidStudioProjects\dw-secret-notes-unity'
# Strings/Widget-Ressourcen aus dem Android-Projekt übernehmen
python "$p\Tools\sync_android_resources.py"
# Selbsttest (Krypto-Testvektoren, Parser, Sprachen)
& $u -batchmode -nographics -projectPath $p -executeMethod DwSecretNotes.EditorTools.SelfTest.RunBatch -logFile "$p\Logs\selftest.log"
# Online-Test gegen domezos-ware.com
& $u -batchmode -nographics -projectPath $p -executeMethod DwSecretNotes.EditorTools.SelfTest.OnlineBatch -logFile "$p\Logs\online.log"
# Android-APK (optional -aab, -development)
& $u -batchmode -nographics -quit -projectPath $p -buildTarget Android -executeMethod DwSecretNotes.EditorTools.ProjectBootstrap.CiBuild -logFile "$p\Logs\build_android.log"
# Windows-Testbuild + Screenshot
& $u -batchmode -nographics -quit -projectPath $p -buildTarget Win64 -executeMethod DwSecretNotes.EditorTools.ProjectBootstrap.CiBuild -dwTarget windows -development
& "$p\Builds\Windows\dw-secret-notes.exe" --shot=E:\shot.png --at=3 --route=main
```

Signierung (Release): Umgebungsvariablen `DW_KEYSTORE`, `DW_KEYSTORE_PASS`, `DW_KEY_ALIAS`, `DW_KEY_PASS`. Ohne diese wird mit dem Debug-Schlüssel signiert.
