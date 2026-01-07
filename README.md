# yt-dlp API

Eine C# ASP.NET Core REST API zum Herunterladen und Archivieren von Videos mit **yt-dlp** und erweiterbaren Konfigurationen.

---

## 📋 Inhaltsverzeichnis

- [Features](#features)
- [Systemanforderungen](#systemanforderungen)
- [Installation](#installation)
- [Konfiguration](#konfiguration)
- [API-Endpoints](#api-endpoints)
- [Docker](#docker)
- [Entwicklung](#entwicklung)

---

## ✨ Features

- **Video-Downloads**: Einfache REST API zum Herunterladen von Videos
- **Konfigurierbare Downloads**: Verschiedene yt-dlp Presets/Konfigurationen verwenden
- **Format-Selektion**: Flexible Video-Format und Qualitäts-Auswahl
- **Post-Processing**: Integrierte Unterstützung für ffmpeg
- **Archivierung**: Automatische Archivierung heruntergeladener Videos
- **Async/Await**: Vollständig asynchrone Operationen für hohe Performance
- **Structured Logging**: Detailliertes Logging mit strukturiertem Format

---

## 🔧 Systemanforderungen

- **.NET 8+** (ASP.NET Core)
- **yt-dlp**: Wird automatisch im Docker-Container installiert
- **ffmpeg**: Wird automatisch im Docker-Container installiert
- **Docker & Docker Compose** (optional, für Container-Betrieb)

---

## 📦 Installation

### Lokal entwickeln

```bash
# Repository klonen
git clone https://github.com/mf450s/yt-dlp.git
cd yt-dlp

# Dependencies installieren
dotnet restore

# Entwicklungsumgebung starten
dotnet run --configuration Debug
```

Die API läuft dann unter `http://localhost:5000`.

### Docker

```bash
# Docker-Image bauen
docker build -t yt-dlp-api .

# Container starten
docker-compose up -d
```

---

## ⚙️ Konfiguration

### Verzeichnisstruktur

```
/app/
├── config/        # yt-dlp Konfigurationsdateien
├── downloads/     # Heruntergeladene Videos
└── archive/       # Archivierte Videos
```

### Konfigurationsdateien

Konfigurationen werden als JSON-Dateien im `config/`-Verzeichnis gespeichert:

```json
{
  "outputPath": "/app/downloads/{title}.%(ext)s",
  "format": "best",
  "audioOnly": false,
  "postProcessors": ["ffmpeg"]
}
```

**Wichtige Optionen:**
- `outputPath`: Ausgabepfad mit yt-dlp Platzhaltern (`{title}`, `{id}`, etc.)
- `format`: Format-Selektor (z.B. `best`, `worst`, `18` für spezifische Formate)
- `audioOnly`: Nur Audio extrahieren (Standard: `false`)
- `postProcessors`: Post-Processing-Tools (z.B. `ffmpeg`)

### Standard-Verzeichnisse

| Verzeichnis | Pfad | Beschreibung |
|------------|------|-------------|
| Downloads | `/app/downloads` | Heruntergeladene Videos |
| Archiv | `/app/archive` | Archivierte Videos |
| Config | `/app/config` | Konfigurationsdateien |

---

## 🔌 API-Endpoints

### Videos herunterladen

```http
POST /api/ytdlp/download
Content-Type: application/json

{
  "videoUrl": "https://www.youtube.com/watch?v=...",
  "configName": "standard"
}
```

**Response:** `202 Accepted`

Der Download wird asynchron im Hintergrund ausgeführt.

---

### Konfigurationen verwalten

#### Liste aller Konfigurationen

```http
GET /api/ytdlp/config/
```

**Response:** Liste der verfügbaren Konfigurationsnamen

```json
[
  "standard",
  "audio-only",
  "high-quality"
]
```

---

#### Spezifische Konfiguration abrufen

```http
GET /api/ytdlp/config/{configName}
```

**Response:** Konfigurationsdaten (JSON)

---

#### Konfiguration erstellen/aktualisieren

```http
POST /api/ytdlp/config/{configName}
Content-Type: application/json

{
  "outputPath": "/app/downloads/{title}.%(ext)s",
  "format": "best",
  "audioOnly": false
}
```

**Response:** `201 Created` oder `200 OK`

---

#### Konfiguration bearbeiten

```http
PATCH /api/ytdlp/config/{configName}
Content-Type: application/json

{
  "format": "worst"
}
```

**Response:** `200 OK`

---

#### Konfiguration löschen

```http
DELETE /api/ytdlp/config/{configName}
```

**Response:** `204 No Content`

---

## 🐳 Docker

### Docker-Compose

```yaml
version: '3.8'

services:
  yt-dlp-api:
    build: .
    container_name: yt-dlp-api
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - downloads:/app/downloads
      - archive:/app/archive
      - configs:/app/configs
      - cookies:/app/cookies
    restart: unless-stopped

volumes:
  downloads:
  archive:
  configs:
  cookies:
```

### Dockerfile

Das Dockerfile nutzt **Multi-Stage Builds** und installiert alle notwendigen Dependencies:

- yt-dlp
- ffmpeg
- Python 3

Das Image wird zu `ghcr.io/mf450s/yt-dlp-api:latest` gepusht.

---

## 👨‍💻 Entwicklung

### Projektstruktur

```
src/
├── Controllers/
│   └── ytdlpController.cs
├── Services/
│   ├── DownloadingService.cs
│   ├── PathParserService.cs
│   └── ConfigsService.cs
├── Models/
│   ├── DownloadRequest.cs
│   └── PathConfiguration.cs
└── Program.cs
```


### Building & Deployment

```bash
# Debug-Build
dotnet build

# Release-Build
dotnet publish -c Release -o ./publish

# Docker-Image bauen und pushen
docker build -t ghcr.io/mf450s/yt-dlp-api:latest .
docker push ghcr.io/mf450s/yt-dlp-api:latest
```