# dw Secret Notes – Unity Edition

Aktuell gepflegte Unity-6-Version der Android-App `dw Secret Notes`. Die Oberfläche basiert auf Unity UI Toolkit; das bisherige Kotlin-/Compose-Repository beschreibt die frühere App. Die Android-App ist unter derselben Paketkennung im [Google Play Store](https://play.google.com/store/apps/details?id=com.snote.domezos) erhältlich.

- Unity: 6000.3.25f1, UI Toolkit (komplett aus C# aufgebaut), Built-in Render Pipeline
- Paket: `com.snote.domezos`, Version 6.1.3 (versionCode 53); am 28.10.2026 in der Play Console als aktiver Produktionsrelease angezeigt

## Funktionen

| Funktion | Umsetzung |
|---|---|
| Text + optionales Bild auf dem Gerät verschlüsseln, Link teilen/kopieren | `Scripts/Crypto/NoteCrypto.cs`, `Scripts/Business/SecretNotesService.cs`, `Scripts/UI/Screens/MainPage.cs` – AES-256-GCM, PBKDF2-SHA256 (100 000 Iterationen) |
| Link/Alias entschlüsseln und entschlüsselte Ansicht nach 60 Sekunden leeren | `Scripts/Business/SecretNotesService.cs`, `Scripts/UI/Screens/MainPage.cs`; nach erfolgreicher Entschlüsselung wird die Serverkopie per `unlink` entfernt und mit einem zweiten `get` im Hintergrund geprüft |
| Kurzlinks/Redirects auflösen | `Scripts/Data/HttpNoteRepository.cs`, `Scripts/Core/LinkParser.cs` |
| Deep Links `https://domezos-ware.com/msges/view.php?...`, Teilen-Intent (Text) | `Plugins/Android/DwActivity.java`, `DwBridge.java`, `Core/LinkParser.cs` |
| 14 Sprachen inkl. Plural-Regeln, RTL (ur) | `Core/Localization.cs`, `Resources/i18n/*.json` |
| 17 Themes | `Core/ThemeCatalog.cs` |
| Hilfe, Info, TinyURL, Sprache, Theme-Auswahl | `UI/Screens/*`, `UI/Shell/AppShell.cs` |
| Homescreen-Widgets (Schnell-Verschlüsseln, Starter) | `Plugins/Android/DwNative.androidlib` |
| Bewertungsdialog (3. Start), In-App-Review, Sterne im Footer | `MainPage.OnShow`, `ReviewHelper.java` |
| FLAG_SECURE in Release-Builds | `DwBridge.setSecure` |
| Spenden-Hinweis (14 Sprachen) | `UI/Screens/DonateCard.cs`; Android: Google Play Billing (`DonationHelper.java`, Produkte `donate_small`, `donate_medium`, `donate_large`, verbrauchbar), Windows: PayPal.me |

Debug-Argumente (Desktop, Debug-Builds auch Android): `--lang=xx`, `--theme=id`, `--route=main|help|info|tinyUrl|language`, `--menu`, `--shot=pfad`, `--e2e` (Verschlüsseln, Kopieren, Teilen, Entschlüsseln gegen das echte Backend, Ergebnis in `persistentDataPath/e2e.txt`).

Neu in der Oberfläche: animierter Aurora-Hintergrund, Glas-Karten, Tabs Verschlüsseln/Entschlüsseln, Vektor-Icons, Countdown-Ring, Einfügen aus Zwischenablage, Bildvorschau mit Vollbild, Bottom-Sheets für Menü/Themes, Akkordeon-FAQ, Toasts.

## Hinweise für Nutzer

- Die App ist ohne Registrierung kostenlos nutzbar. Auf Android sind freiwillige Spenden über Google Play Billing möglich; sie sind für die Kernfunktionen nicht erforderlich.
- Text und ein optionales Bild werden vor dem Hochladen auf dem Gerät mit AES-256-GCM verschlüsselt. Der erzeugte Link enthält Informationen, die zum Entschlüsseln benötigt werden; teile ihn nur mit der vorgesehenen Person.
- Nach erfolgreicher Entschlüsselung sendet die App `unlink` an das PHP-Backend. Anschließend prüft ein zweiter, für Nutzer unsichtbarer `get`-Abruf, ob der Server `not_found` meldet. Das Ergebnis wird protokolliert; der Countdown und die Anzeige der entschlüsselten Nachricht laufen unabhängig davon 60 Sekunden weiter. Die zusätzliche Prüfung ist eine Quellcodeänderung und wird erst mit einem neuen App-Build ausgeliefert.
- Die App-Oberfläche bietet 14 Sprachen. Bei anderen Gerätesprachen wird Englisch verwendet. Der Store-Eintrag kann zusätzlich in weiteren Sprachen lokalisiert sein.

UI/Accessibility: UI Toolkit verwendet vorab erzeugte TextCore-SDF-FontAssets unter `Assets/DwSecretNotes/Fonts` (nicht `Resources`); die Schriftquellen bleiben dort für die Lizenz-/Asset-Reproduzierbarkeit. Fließtext nutzt kontrastgeprüfte, abgetönte Farben (mindestens WCAG AA 4,5:1), Neonfarben bleiben Akzenten vorbehalten. Notiz- und Linkfelder sind standardmäßig maskiert und bieten lokalisierte Ein-/Ausblenden- und Leeren-Schaltflächen. Klartext wird bewusst nicht automatisch gespeichert; geheime Entwürfe bleiben im Arbeitsspeicher, bis sie verschlüsselt oder gelöscht werden. Das UI-Toolkit-Panel skaliert mit Referenzauflösung 1080×1920 und Width/Height-Match 0,5.

Unterschied: Das Widget öffnet die App mit einem Schnell-Verschlüsseln-Sheet statt eines transparenten Overlays über dem Homescreen.

## Struktur

```
Assets/DwSecretNotes/Scripts/Core      Logging, App-Konfiguration, L10n, Themes, Link-Parser
Assets/DwSecretNotes/Scripts/Crypto    PBKDF2-SHA256, AES-256-GCM, Payload
Assets/DwSecretNotes/Scripts/Business  SecretNotesService, Use-Cases und Repository-Vertrag
Assets/DwSecretNotes/Scripts/Data      HTTP-Repository und lokale PlayerPrefs
Assets/DwSecretNotes/Scripts/Platform  Native Bridge (Android / Desktop)
Assets/DwSecretNotes/Scripts/UI        Shell, Komponenten, Seiten
Assets/DwSecretNotes/Scripts/Bootstrap Composition Root und Runtime-Start
Assets/DwSecretNotes/Editor            Editor-only Setup, Builds, Picker und Selbsttests
Assets/DwSecretNotes/Editor/Tooling    Android-Resource-Sync
Assets/DwSecretNotes/Editor/Windows    Windows-Installer-Skript
Assets/Plugins/Android                 Activity, Bridge, Widgets, Bildauswahl
```

Die Runtime-Schichten sind durch separate Assembly-Definitions (`Core`, `Crypto`, `Business`, `Data`, `Platform`, `UI`, `Bootstrap`) voneinander abgegrenzt. Die UI nutzt Notiz-Use-Cases über `SecretNotesService`; Storage/HTTP und PlayerPrefs sind hinter Repository-Verträgen gekapselt. Editor-APIs liegen ausschließlich unter `Assets/DwSecretNotes/Editor` und sind nicht aus Runtime-Assemblies referenziert.

## Befehle

```powershell
$u = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe'
$p = 'E:\AndroidStudioProjects\dw-secret-notes-unity'
# Strings/Widget-Ressourcen aus dem Android-Projekt übernehmen
python "$p\Assets\DwSecretNotes\Editor\Tooling\sync_android_resources.py"
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

## Release- und Kompatibilitätsprüfung

Die folgenden Prüfnachweise stammen vom Entwicklungsstand 6.1.2 am 25.10.2026. Sie sind kein vollständiger neuer Testbericht für den am 28.10.2026 in der Play Console aktiven Produktionsrelease 6.1.3 (53). Nach der Aktualisierung der Hilfetexte wurde der Unity-SelfTest am 28.10.2026 lokal erneut ausgeführt (`RESULT fails=0`); sein Protokoll liegt unter `Logs/docs-audit-localization-2026-10-28.log` und ist nicht versioniert.

- Backend-Kompatibilität: `Tools/CryptoBackendCompatibility` testet PBKDF2/AES-GCM-Vektoren sowie Text- und Bildnachrichten gegen das echte Backend. Der aktuelle Unity-Android-Laufzeitcheck bleibt bewusst auf der bestehenden Custom-Krypto, da `AesGcm` auf dem Zielgerät nicht verfügbar ist.
- Accessibility: `ScreenReaderBridge` registriert fokussierbare UI-Elemente in Unitys Accessibility-Hierarchie; Android-TalkBack wurde auf dem Emulator mit protokollierten Hierarchieknoten geprüft.
- Bewegung: Android `animator_duration_scale` steuert den Reduced-Motion-Modus; Aurora, Countdown- und UI-Übergänge werden dann angehalten bzw. deaktiviert.
- Verifikation: `Logs/selftest-final2.log` (`RESULT fails=0`), `Logs/build-windows-final.log` (Build erfolgreich), Windows-E2E `persistentDataPath/e2e.txt` (`RESULT PASS`) und Android-Emulator-Smoke `Logs/android-phase6-smoke.png`.

## Backend-Status und Abbruch

`HttpNoteRepository` wertet für `action=get` den JSON-Status `not_found` als `NoteFetchStatus.NotFound` aus. Ein erfolgreicher Payload wird als `Found` weitergegeben; Transportfehler und fehlende Payloads werden als `Failed` behandelt. Für `action=save` gilt HTTP 429 als `RateLimited`. Der Inhalt einer entschlüsselten Nachricht wird nicht als Statusmeldung interpretiert.

Die UI bricht laufende Verschlüsselungs- und Entschlüsselungsanfragen beim Verlassen der Hauptseite und bei App-Hintergrund ab. Das stoppt den Client-Request; ein bereits vom PHP-Backend verarbeiteter Schreib- oder Löschvorgang wird dadurch nicht zurückgenommen.
