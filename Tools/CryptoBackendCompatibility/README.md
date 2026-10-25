# Krypto- und Backend-Kompatibilität

Diese .NET-9-Testanwendung vergleicht die bestehenden Unity-Implementierungen von PBKDF2-SHA256 und AES-GCM mit den .NET-Standardbibliotheken. Sie speichert danach zwei eigene Text-/Bild-Testnachrichten über `https://domezos-ware.com/api/msg_store.php`, liest sie zurück, prüft beide Entschlüsselungsrichtungen und entfernt die Testnachrichten wieder. Sie gibt weder API-Schlüssel noch Passwörter oder Klartext aus. Die Ergebnisse stehen in `Logs/crypto-backend-dotnet.txt`.

```powershell
$env:DW_SECRET_NOTES_API_KEY = '<Schlüssel nur für diesen Prozess bereitstellen>'
dotnet run --project Tools/CryptoBackendCompatibility/CryptoBackendCompatibility.csproj
Remove-Item Env:DW_SECRET_NOTES_API_KEY
```

Der Test benötigt Netzwerkzugriff und verändert nur selbst erzeugte Testnachrichten. Ein `RESULT PASS` bestätigt die Kompatibilität des Nachrichtenformats mit dem Backend. Er bestätigt **nicht**, dass Unity/IL2CPP die .NET-Krypto-APIs auf Android ausführen kann.

Der ergänzende Unity-Editor-Test wird so gestartet:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'E:\AndroidStudioProjects\dw-secret-notes-unity' -executeMethod DwSecretNotes.EditorTools.CryptoBackendCompatibility.RunBatch -logFile 'E:\AndroidStudioProjects\dw-secret-notes-unity\Logs\crypto-backend-unity.log'
```

Am 25.10.2026 bestand der .NET-9-Test alle lokalen und Backend-Prüfungen. Der Unity-Editor-Test scheiterte bereits vor dem Backend-Zugriff: `Rfc2898DeriveBytes` akzeptiert den bisherigen vier Byte langen Salt nicht; `AesGcm` löst in der installierten Unity-Laufzeit `PlatformNotSupportedException` aus. Daher wurde die Krypto-Umstellung aus Audit-Schritt 1.2 übersprungen.
