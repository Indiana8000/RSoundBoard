# Soundboard Anwendung - .NET 8

## 🎯 Features

- **Weboberfläche**: Responsive Soundboard über LAN erreichbar (http://localhost:5000)
- **Lokale Audio-Wiedergabe**: Sounds werden nur auf dem Windows-PC abgespielt (NAudio)
- **Desktop Manager**: WinForms UI zur Verwaltung der Buttons
- **Single-File EXE**: Vollständig selbstständige Anwendung
- **Unterstützte Formate**: WAV, MP3

## 📦 Projektstruktur

```
TestApp1/
├── Program.cs                  # Haupteinstiegspunkt (Webserver + WinForms)
├── Models/
│   └── SoundButton.cs          # Datenmodell
├── Services/
│   ├── ButtonRepository.cs     # JSON-basierte Datenverwaltung
│   └── SoundService.cs         # NAudio Audio-Player
├── HostUI/
│   ├── MainForm.cs             # Hauptfenster (Verwaltung)
│   └── ButtonEditDialog.cs    # Dialog zum Hinzufügen/Bearbeiten
└── wwwroot/
    ├── index.html              # Web-Soundboard
    └── style.css               # Styling

soundboard_data.json            # Automatisch erstellt beim Start
```

## 🚀 Build & Publish

### Entwicklung testen
```bash
dotnet restore
dotnet run
```

### Single-File EXE erstellen
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Die fertige EXE befindet sich in:
```
TestApp1\bin\Release\net8.0-windows\win-x64\publish\TestApp1.exe
```

## 🎮 Verwendung

1. **Anwendung starten**: `TestApp1.exe` ausführen
2. **Desktop-UI öffnet sich**: Hier können Buttons verwaltet werden
3. **Buttons hinzufügen**:
   - "Hinzufügen" klicken
   - Label eingeben
   - Sound-Datei auswählen (WAV/MP3)
   - Gruppe angeben
   - Reihenfolge festlegen
4. **Weboberfläche öffnen**: Button "Weboberfläche öffnen" im Desktop-UI
5. **Von anderen Geräten zugreifen**: `http://<PC-IP>:5000` im Browser öffnen

## 📡 API Endpoints

- `GET /api/buttons` - Alle Buttons abrufen
- `POST /api/play/{id}` - Sound abspielen
- `POST /api/stop` - Sound stoppen
- `POST /api/buttons` - Button hinzufügen
- `PUT /api/buttons/{id}` - Button aktualisieren
- `DELETE /api/buttons/{id}` - Button löschen

## 💾 Datenspeicherung

Buttons werden in `soundboard_data.json` im gleichen Verzeichnis wie die EXE gespeichert.
Die Datei wird automatisch erstellt, wenn sie nicht existiert.

### Beispiel JSON
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "label": "Applaus",
    "filePath": "C:\\Sounds\\applause.wav",
    "group": "Effekte",
    "order": 0
  }
]
```

## ⚙️ Technische Details

- **Framework**: .NET 8 (Windows)
- **Webserver**: Kestrel (ASP.NET Core Minimal API)
- **Audio**: NAudio 2.2.1
- **UI**: Windows Forms
- **Port**: 5000 (HTTP, kein HTTPS für LAN)
- **Netzwerk**: Lauscht auf allen Interfaces (0.0.0.0)

## 🔧 Konfiguration

Die Anwendung benötigt:
- Windows 10/11
- Keine zusätzlichen Frameworks (Self-Contained)
- Zugriff auf lokale Sound-Dateien
- Firewall-Regel für Port 5000 (für LAN-Zugriff)

## 🎨 Features im Detail

### Audio-Verhalten
- Nur ein Sound gleichzeitig
- Neuer Sound stoppt automatisch den aktuellen
- Sauberes Dispose-Pattern für NAudio-Ressourcen

### Web-Soundboard
- Responsive Design für Desktop & Mobile
- Auto-Refresh alle 5 Sekunden
- Gruppierung mit visueller Trennung
- Große, klickbare Button-Kacheln

### Desktop Manager
- Einfache Liste aller Buttons
- Buttons hinzufügen/bearbeiten/löschen
- Dateiauswahl-Dialog für Sound-Dateien
- Reihenfolge ändern (Auf/Ab)
- Direkt zum Browser wechseln

## 📝 Hinweise

- Audio wird **nur lokal** auf dem Host-PC abgespielt
- Browser sendet nur HTTP-Requests, kein Audio-Streaming
- Keine Authentifizierung (nur für vertrauenswürdige LANs)
- JSON-Datei wird automatisch bei jeder Änderung gespeichert
