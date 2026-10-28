# Audit-Umsetzung — dw-secret-notes-unity v6.1.1

> Historisches Arbeitsprotokoll vom 25.10.2026. Versionsnummern, offene Aufgaben und Freigabevermerke unten beschreiben den damaligen Stand. Aktueller Quell- und Produktionsstand: 6.1.3 (versionCode 53), am 28.10.2026 in der Play Console geprüft. Für den aktuellen Funktionsumfang siehe [README.md](README.md).

> **Quelle:** Code-Reviewer (Stages 5–8) + UX-UI Auditor (Phases 0–5)  
> **Projekt:** Unity 6000.3.25f1, Package com.snote.domezos v6.1.1  
> **Ziel:** Alle im Audit gefundenen Probleme methodisch angehen — von kritischen Security-Löchern bis zu UI-Verbesserungen.  
> **Format:** Jeder Schritt ist ein abhakbares [ ]-Item. Phasen sind chronologisch geordnet, aber unabhängig ausführbar.

---

## 🔴 PHASE 1 — Kritische Security & Robustheit (Sofort!)

### 1.1 Hardcoded Secrets entfernen

**Status 25.10.2026:** Auf ausdrücklichen Wunsch übersprungen. Die folgenden Arbeitspakete bleiben offen.

Lese Dir zuerst die folgenden Schritte durch, ändere noch nichts!! Sage mir bevor Du diese Schritte abarbeitest ob es eine Lösung gibt die sicher ist damit nicht jeder Nutzer der App ApiKeys, BaseUrls, Fallbackpassphrase, PackageIds oder PayPalUrl selbst in die App eintragen muss? Diese Informationen sind App intern. Die App muss auch noch trotz der Lösung mit meinem Backend auf `https://domezos-ware.com` klar kommen. `appsettings.json` im Editor einrichten für Tests ist kein Problem aber es geht um die Laufzeit der release Build die ja vom Google Playstore direkt geldaen und installiert wird. Ich kann auch schlecht die den standard User mitteilen zum eintragen. Gib mir 3 sichere Varianten und frage zuerst. Wenn mir keine dieser Varianten gefällt sage Ich Dir diese Schritte zu überspringen und mit 1.2 weiter zu machen.

- [ ] AppInfo.cs löschen: ApiKey, BaseUrl, FallbackPassphrase, PackageId, PayPalUrl
- [ ] Diese Werte über **Environment Variables** oder **Player Settings** zur Laufzeit zuführen
- [ ] Für Tests appsettings.json im Editor einrichten (nicht ins Repo commiten)
- [ ] Validieren: Kein string.IsNullOrEmpty-Check mehr nötig, wenn Werte von aussen kommen → neue Fehlerquelle prüfen

### 1.2 Standard-Krypto-Bibliotheken einsetzen

**Status 25.10.2026:** Nach der Kompatibilitätsprüfung auf Wunsch übersprungen. Das Format bestand den echten Backend-Test in beide Richtungen (`Logs/crypto-backend-dotnet.txt`). In Unity lehnt `Rfc2898DeriveBytes` den bisherigen vier Byte langen Salt ab und `AesGcm` wirft `PlatformNotSupportedException` (`Logs/crypto-backend-compat.txt`). Testanwendung: `Tools/CryptoBackendCompatibility/`.

Lese Dir zuerst die folgenden Schritte durch, ändere noch nichts!! Ist die Umstellung überhapt möglich so damit das Encrypten und Decrypten mit der Seite `https://domezos-ware.com` kompatible bleibt? Nenne mir hierzu die Antwort und frage mich zuerst ob Du diese Schritte vornehmen sollst oder weiter zu 1.3 gehen sollst.

- [ ] AesGcm256.cs vollständig entfernen durch System.Security.Cryptography.AesGcm (.NET 8+ / Unity 6)
- [ ] Pbkdf2Sha256.cs entfernen durch Rfc2898DeriveBytes mit korrektem Salt/Iterations-Handling
- [ ] Falls Custom-Implementierung bleiben muss: unit tests für NIST-Vector TC15 und RFC 7914 hinzufügen (ähnlich SelfTest)
- [ ] SelfTest.cs an neue Bibliothek anpassen und bestehendes Test-Vector-Ergebnis bestätigen

### 1.3 async void Bereinigung

- [x] SelfTest.Run (Zeile 24) in async Task umwandeln und ConfigureAwait(false) hinzufügen
- [x] AppBootstrap.E2E (Zeile 70) in async Task umwandeln; falls als Editor-MenuItem: Task.Run Wrapper
- [x] Alle neuen async-Methoden ab sofort nur mit Task Return-Type (ausser echte Event-Handler)

### 1.4 Cancellation Tokens einbauen

Vor Du diese Schritte durchführst beantworte mir folgende Frage und frage mich ob Du diese Schritte machen sollst oder gleich weiter zu 1.5 gehen sollst. Mein Frage: Muss für diese Schritte im .php Backend etwas geändert werden?

- [x] WebRequestAwaiter.Send CancellationToken parameter hinzufügen; Caller (HttpNoteRepository) weiterleiten
- [x] MainPage HTTP-Calls (Encrypt/Decrypt) CancellationTokenSource bereitstellen; bei Szenenwechsel oder App-Hintergrund Cancel() aufrufen
- [x] NativeBridge-Methoden geprüft: Die hier genutzten ADB/Win32-Aufrufe sind keine asynchronen HTTP-Aufrufe; ein CancellationToken ist dafür nicht erforderlich.

### 1.5 Deutsch-String aus Business-Logik entfernen

- [x] SecretNotesService.FetchAndDecrypt Zeile 105: ote.Text.Contains("Nachricht nicht gefunden") durch Status-Code-Check des Payloads ersetzen
- [x] Falls Server-Status-Codes genutzt werden: Doku im Repository aktualisieren

---

## 🔴 PHASE 2 — Kritische Barrierefreiheit (Accessibility First!)

**Status 25.10.2026:** Umgesetzt und auf Android-Emulator sowie Windows-E2E geprüft. TalkBack meldete die Unity-Accessibility-Knoten; Tastatur-Tab, Enter und Escape wurden im Emulator ausgeführt.

### 2.1 Tastatur-/TV-Fokus komplett nachrüsten

Hier achtung geben damit alles noch mit Android kompatible bleibt.

- [x] **USS (dw.uss):** Fokus-Ringe für alle interaktiven Elemente hinzufügen:
  `
  .dw-btn:focus-visible, .dw-field:focus-within, .dw-tab:focus-visible,
  .dw-menuitem:focus-visible, .dw-iconbtn:focus-visible, .dw-langitem:focus-visible,
  .dw-star:focus-visible, .dw-swatch:focus-visible, .dw-dialog:focus-visible,
  .dw-sheet:focus-visible
  { outline: 2px solid var(--focus-ring-color, #00D4FF); outline-offset: 2px; }
  `
- [x] --focus-ring-color per ThemeBinder aus Primary ableiten: ThemeBinder.A(p.Primary, 0.8f)
- [x] **C#:** In Kit.cs / DwButton / DwField / Tab / IconButton / MenuItem: ocusable = true; sicherstellen (bzw. ocusable: true in USS explizit setzen)
- [x] **AppShell.cs / Page.cs:** NavigationMoveEvent / NavigationSubmitEvent / NavigationCancelEvent registrieren und Fokus-Management implementieren (Unity UI Toolkit Navigation API)
- [x] **Test:** TAB durch alle Elements auf Desktop/TV, ENTER triggert Click, ESCAPE schließt Overlay/Back

### 2.2 Thumbnail Remove-Button Touch-Target vergrößern

- [x] **USS (dw.uss Zeile 84):** .dw-thumb__remove ändern:
  `
  .dw-thumb__remove { width: 32px; height: 32px; right: -10px; top: -10px; min-width: 44px; min-height: 44px; padding: 6px; }
  `
- [x] C# (MainPage.cs): Icon-Größe auf 16px erhöhen (bisher 12), vertikal/horizontal zentrieren
- [x] Test: Auf Touch-Gerät 44×44px Bereich antippen, darf Text/Icon darunter nicht treffen

### 2.3 Screen-Reader-Accessibility grundlegend

Hier auch darauf achten das alles Android kompatible bleibt

- [x] **C#:** Allen interaktiven Elementen iewDataKey setzen (z.B. "btn-encrypt", "tab-decrypt", "field-input", "menu-theme", "star-rate")
- [x] **Icons mit pickingMode=Ignore:** Semantische Labels via Tooltip ergänzen
- [x] **DwButton:** Alle Buttons haben bereits 	ooltip (Constructor Parameter). Prüfen: Jeder Button hat Text ODER Icon+Tooltip vollständig
- [x] **DwField:** 	ooltip = hint (Zeile 52 Kit.cs) — bestätigen, bei Bedarf erweitern
- [x] **Accordion-Head:** 	ooltip hinzufügen: "Expand/Collapse Section"
- [x] **Footer Stars:** 	ooltip = "Rate {n} stars" (bzw. localisation-key)
- [x] **ThemeBinder:** Bei Theme-Wechsel iewDataKey nicht verlieren

---

## 🟠 PHASE 3 — Hochpriorierte Maintenance & UI-Verbesserungen

**Status 25.10.2026:** Umgesetzt; SelfTest `fails=0`, Android-Player-Build erfolgreich.

### 3.1 Spacing & Radius Token-System einbauen

- [x] **ThemeCatalog.cs:** ThemePalette um SpacingTokens (Dictionary<string,float>) und RadiusTokens (Dictionary<string,float>) erweitern:
  - Standard: spacing-1=4, spacing-2=8, spacing-3=12, spacing-4=16, spacing-5=24, spacing-6=32, spacing-7=48
  - radius-xs=4, radius-sm=8, radius-md=12, radius-lg=16, radius-xl=24, radius-full=9999
- [x] **USS (dw.uss):** Custom Properties definieren (Unity 2022.2+ unterstützt ar()):
  `
  :root { --sp-1: 4px; --sp-2: 8px; --sp-3: 12px; --sp-4: 16px; --sp-5: 24px; --sp-6: 32px; --sp-7: 48px;
          --rad-xs: 4px; --rad-sm: 8px; --rad-md: 12px; --rad-lg: 16px; --rad-xl: 24px; --rad-full: 9999px; }
  `
- [x] Alle hardcoded Werte in dw.uss durch Tokens ersetzen (z.B. padding: var(--sp-4) var(--sp-4) var(--sp-7) var(--sp-4);)
- [x] **C#:** Kit.Spacer(float h) bleibt kompatibel; ThemeBinder.GetSpacing(token) ist ergänzt

### 3.2 TinyUrlPage auf Design-System migrieren

- [x] **TinyUrlPage.cs:** Alle hardcoded Colors (Navy900, Cyan, Gold, White, Muted, Glass) entfernen
- [x] Stattdessen ThemeBinder.On(root, (e,p) => { ... }) nutzen mit p.Primary, p.Secondary, p.AccentText, p.Background, p.Surface, p.SurfaceVariant
- [x] **USS-Klassen** für TinyUrl-spezifische Styles in dw.uss ergänzen (.dw-tiny, .dw-tiny__card, .dw-tiny__badge, .dw-tiny__num, .dw-tiny__step)
- [x] **RTL-Support:** Root?.EnableInClassList("dw-rtl", L10n.IsRtlTag(selected)) wie in LanguagePage
- [x] Hardcoded String "AES-256-GCM · PBKDF2-SHA256" → L10n Key info_crypto_algo hinzufügen und nutzen

### 3.3 Hero-Ad Carousel Pause-on-Hover/Focus (WCAG 2.2.2)

- [x] **MainPage.cs:** adBox Variable existiert. schedule.Execute(...).Every(5500) zurückgeben (IVisualElementScheduledItem adTimer)
- [x] adBox.RegisterCallback<MouseEnterEvent>(_ => adTimer.Pause()), MouseLeaveEvent → adTimer.Resume()
- [x] FocusEvent / BlurEvent analog für Tastatur-Navigation
- [x] adBox.focusable = true; setzen

### 3.4 prefers-reduced-motion Support

- [x] **AppBootstrap.cs:** Systemweite Android-Einstellung `animator_duration_scale` über JNI prüfen; Unity 6 stellt keine `AccessibilitySettings.reduceMotion`-API bereit.
- [x] Global speichern: ThemeBinder.SetReducedMotion(reduceMotion) (neue statische Property in ThemeBinder)
- [x] **ThemeBinder.cs:** public static bool ReducedMotion { get; private set; } + SetReducedMotion(bool v) implementieren
- [x] **Aurora.cs:** In Tick() → if (ThemeBinder.ReducedMotion) return
- [x] **CountdownRing.cs:** Bei ReducedMotion wird der Fortschrittswert auf 0 gesetzt; Spinner aktualisiert nicht.
- [x] **USS:** `.dw-reduced-motion` deaktiviert Übergangsdauern; Klasse wird per C# gesetzt.

### 3.5 Toast-Queue statt Single-Toast

- [x] **AppShell.cs:** 	toastHost → List<VisualElement> toastQueue = new(); int maxToasts = 3;
- [x] Toast(string msg): Neues Toast-Element erstellen, toastQueue.Add(toast), toastHost.Add(toast)
- [x] Position: 	translate: 0 calc(20px + index * 70px) für Staggering (USS: .dw-toast:nth-child(n) { translate: 0 calc(20px + (n-1)*70px); })
- [x] Auto-Dismiss: Nach 2.6s RemoveFromClassList("dw-toast--hidden") → AddToClassList → RemoveFromHierarchy() → 	toastQueue.Remove(toast) → verbliebene Toasts nach oben animieren (Kit.Enter mit translate)
- [x] toastHost.Clear() entfernen

---

## 🟡 PHASE 4 — Mittlere Priorität (Nice-to-Have / QOL)

**Status 25.10.2026:** Umgesetzt; Bildbetrachter mit Drag, Wheel-Zoom, Begrenzung und Doppeltipp-Reset. Die Unity-Version stellt keinen separaten Pinch-Event bereit.

### 4.1 Image Viewer Pan/Zoom

- [x] **AppShell.cs ShowImage():** dw-viewer__img Manipulatoren hinzufügen:
  - PointerManipulator für Drag (translate), WheelEvent / PinchGesture für Zoom (scale)
  - Min/Max Scale (1.0 – 5.0), Clamp Translate bei Zoom > 1
  - Double-Tap → Reset auf 1.0 / Center
- [x] **USS:** .dw-viewer__img { transform-origin: center center; transition: transform 100ms ease-out; }

### 4.2 Accordion Chevron Rotation 180°

- [x] **USS (dw.uss Zeile 144-145):** .dw-accordion__chev { transition-property: rotate; transition-duration: 200ms; }
  - .dw-accordion--open .dw-accordion__chev { rotate: 180deg; } (statt 45deg)
- [x] IconKind.Plus als Basis → 180° = Minus. Visuell konsistent.

### 4.3 Footer Stars Disabled-State bei HasRatedApp

- [x] **Footer.cs Fill():** Wenn preferences.HasRatedApp:
  - stars.pickingMode = PickingMode.Ignore; stars.style.opacity = 0.5;
  - Jeden Stern: 	ooltip = "Already rated";
- [x] **USS:** .dw-star:disabled { opacity: 0.5; } (wenn EnableInClassList("disabled", true) genutzt)

### 4.4 LanguagePage Button-Text Overflow

- [x] **LanguagePage.cs Zeile 46:** confirm.Text.text = ... — langen NativeName abkürzen oder separaten Label
- [x] Fix: confirm.style.flexWrap = Wrap.Wrap; + confirm.Text.style.maxWidth = 200; +	text-overflow: ellipsis; overflow: hidden; white-space: nowrap; (USS auf .dw-btn__text bereits vorhanden, aber lex-shrink: 1 prüfen)
- [x] Separater Label ist nicht erforderlich: `maxWidth`, `flexShrink` und USS-Ellipse verhindern Überlauf innerhalb des Buttons.

### 4.5 Plural Keys

ote_auto_delete_one/other in JSON prüfen & ergänzen

- [x] en.json / de.json / etc.: Keys 
ote_auto_delete_one ({0} second remaining) + 
ote_auto_delete_other ({0} seconds remaining) sicherstellen
- [x] MainPage.cs Zeile 268: L10n.Plural("note_auto_delete", sec) nutzt diese Keys automatisch

### 4.6 QuickEncryptSheet Auto-Focus Timing optimieren

- [x] **QuickEncryptSheet.cs Zeile 41:** s.schedule.Execute(() => field.Input.Focus()).StartingIn(300); entfernen/verschieben
- [x] **AppShell.cs OpenOverlay() Zeile 118:** panel.RemoveFromClassList("dw-panel--hidden") → **hier** Focus setzen (Overlay sichtbar)
- [x] OpenOverlay Callback onOpened ergänzen oder panel.RegisterCallback<GeometryChangedEvent> (einmalig) → ield.Input.Focus()

---

## 🟢 PHASE 5 — Niedrige Priorität (Polish / Optimierung)

**Status 25.10.2026:** Umgesetzt; SelfTest `fails=0`, Android-Player-Build erfolgreich.

### 5.1 Aurora Performance: Nur auf MainPage aktivieren

- [x] In AppBootstrap.cs: staticRoot.Add(new Aurora()) → entfernen
- [x] MainPage BuildHero() oder Build() → c.Add(new Aurora()) (oder separater Layer)
- [x] Aurora-Instanz bei Szenenwechsel staticRoot sauber löschen

### 5.2 CountdownRing Value-Label redundant entfernen/bewerten

- [x] CountdownRing Value Label (dw-ring__value) entfernen (style.display = DisplayStyle.None;) **oder** nur Ring nutzen
- [x] Accessibility: Text für SR behalten via ria-label äquivalent, wenn Label entfernt wird

### 5.3 Theme-Sheet Swatch Hover-Check

- [x] USS .dw-swatch:hover .chk { display: flex; opacity: 0.5; } (.chk ist Icon Check)
- [x] Test: Mouse-hover auf Swatch im Theme-Sheet, Check taucht sanft auf

### 5.4 DonateCard Loading-Skeleton

- [x] dw-skeleton USS-Klasse (shimmer animation via @keyframes oder USS Transition auf opacity/ackground-color)
- [x] In QueryDonations Callback: Skeleton anzeigen bis offers geladen, dann ausblenden

### 5.5 InfoPage Crypto-String lokalisieren

- [x] L10n Key info_crypto_algo hinzufügen, in InfoPage.cs L10n.T("info_crypto_algo") nutzen
- [x] Key in allen 14 Sprach-JSONs eintragen

---

## 🟢 PHASE 6 — get ready and publish

**Status 25.10.2026:** Windows-E2E und Android-Emulator-Smoke erfolgreich. Version auf 6.1.2 / versionCode 51 angehoben und README geprüft. Der aktuelle Android-Release-Build (APK/AAB) bleibt offen: Unity Bee `Player` endet wiederholt mit ExitCode 4 und erzeugt kein neues Artefakt; der vorherige Entwicklungs-/A11y-Build bleibt reproduzierbar erfolgreich. Play-Console-Publishing und Git-Push/Merge sind bis zur ausdrücklichen Freigabe nicht ausgeführt.

- [x] starte die App im Emulator und durch Klicks und Tastenschläge alles testen und falls alles funktioniert zum nächsten Schritt, falls nicht: debuggen; Ursachen finden und lösen und nochmal Phase 6 anfangen
- [x] Raise App Version for Google Play Console
- [ ] build Releases and when done check to see if it is there (offen: Unity Bee Player ExitCode 4 beim aktuellen Release-Build)
- [x] update Docs and when finisched read Docs to see if Change took place
- [ ] publish Release on Google Play Console adn when dont look to see if all Changes actually took place
- [ ] github - commit all changes, push and merge

## 📋 QUICK-START-CHECKLISTE (Alle Phasen)

`
[ ] C-01: Hardcoded Secrets aus AppInfo.cs entfernen → Env Vars / Player Settings
[ ] C-02: AesGcm256 + Pbkdf2Sha256 durch Standard-.NET ersetzen + SelfTest aktualisieren
[x] C-03: async void → async Task (SelfTest.Run, AppBootstrap.E2E)
[x] C-04: Cancellation Tokens in WebRequestAwaiter + HttpNoteRepository einbauen
[x] C-05: "Nachricht nicht gefunden" String aus Business-Logik entfernen → Status Code Check
[ ] C-06: Fokus-Ringe in dw.uss für alle interaktiven Elemente addieren
[ ] C-07: focusable = true auf allen C# UI-Elementen bestätigen
[ ] C-08: NavigationMove/Submit/Cancel Events in AppShell/Page implementieren
[ ] C-09: Thumbnail Remove-Button 44×44px Touch-Target (USS + C#)
[ ] C-10: viewDataKey + tooltip auf allen Interaktiven + Icons setzen
[ ] H-01: ThemeCatalog + USS Spacing/Radius Tokens einbauen
[ ] H-02: TinyUrlPage auf ThemeBinder + L10n migrieren
[ ] H-03: Hero-Ad Carousel Pause-on-Hover/Focus (WCAG)
[ ] H-04: prefers-reduced-motion Check in AppBootstrap + ThemeBinder + Aurora/CountdownRing
[ ] H-05: Toast-Queue (max 3, staggered) statt Single-Toast
[ ] M-01: Image Viewer Pan/Zoom (PointerManipulator)
[ ] M-02: Accordion Chevron 180° Rotation
[ ] M-03: Footer Stars disabled + tooltip "Already rated" bei HasRatedApp
[ ] M-04: LanguagePage Button-Text Overflow-Lösung
[ ] M-05: Plural Keys note_auto_delete_one/other in JSONs ergänzen
[ ] M-06: QuickEncryptSheet Auto-Focus nach OpenOverlay setzen
[ ] L-01: Aurora nur auf MainPage aktivieren (staticRoot bereinigen)
[ ] L-02: CountdownRing Value-Label redundanz prüfen/entfernen
[ ] L-03: Theme-Sheet Swatch Hover-Check (USS .dw-swatch:hover .chk)
[ ] L-04: DonateCard Loading-Skeleton implementieren
[ ] L-05: InfoPage Crypto-String lokalisieren (L10n Key)
[ ] 06: get ready and publish
`

---

## 📌 WICHTIGE ARCHITEKTUR-REGELN FÜR DIE UMSETZUNG

1. **Theme-System:** ThemeCatalog.cs (Oklab, ColorMath) → ThemeBinder.cs (Bindings) → USS (Custom Props ar(--token))
2. **L10n:** L10n.T(key) / L10n.Plural(key, n) — **KEINE harten Strings** mehr im UI-Code
3. **RTL:** Root.EnableInClassList("dw-rtl", L10n.IsRtl) + USS .dw-rtl Regeln
4. **Haptik:** Native.I.Haptic(Haptic.LongPress) auf **JEDEM** Clickable (bzw. C#-Nachprüfung)
5. **Safe Area:** AppShell.ApplySafeArea() in Update() + GeometryChangedEvent auf Root
6. **Fonts:** FontService.Apply/ApplyLanguage (Noto Sans SDF + Fallbacks pro Schriftsystem)
7. **Tests:** AppBootstrap.E2E() für Critical Paths nach jedem größeren Change neu laufen lassen
8. achte IMMER darauf das alle änderungen Android kompatible sind

---

**Ende der Datei.** Jedes [ ] ist ein abhakbares Arbeitspaket. Empfohlene Reihenfolge: Phasen 1–3 (Critical + High) in einem Sprint, danach Phasen 4–5 (Medium + Low) nach und nach. Nach Phase 1 und 2 unbedingt SelfTest.cs und die E2E-Tests (AppBootstrap.E2E()) erneut durchlaufen, um keine Regressionsfehler einzubauen.
