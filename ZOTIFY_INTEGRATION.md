# Zotify Integration Guide

Diese Dokumentation erklärt die Zotify-Integration in das yt-dlp-docker-backend Projekt.

## Überblick

Zotify ermöglicht es, Spotify-Inhalte (Tracks, Playlists, Alben) zu downloaden. Die Integration ist minimal invasiv und nutzt die bestehende Architektur des Projekts.

### Unterstützte URLs

Das System erkennt automatisch den Inhaltstyp anhand der URL:

- **Spotify URLs** → Zotify
  - `https://open.spotify.com/track/...`
  - `https://open.spotify.com/playlist/...`
  - `https://open.spotify.com/album/...`
  - `https://spotify.com/...`

- **Andere URLs** → yt-dlp
  - `https://www.youtube.com/watch?v=...`
  - `https://www.twitch.tv/...`
  - Alle anderen Quellen

## Setup

### 1. Zotify Konfiguration erstellen

Zotify-Konfigurationsdateien werden als JSON-Dateien im `/app/configs/` Verzeichnis gespeichert.

**Beispiel:** `/app/configs/spotify-default.json`

```json
{
  "ROOT_PATH": "/app/downloads",
  "DOWNLOAD_FORMAT": "mp3",
  "DOWNLOAD_QUALITY": "high",
  "SKIP_EXISTING_FILES": true,
  "DOWNLOAD_REAL_TIME": false,
  "OUTPUT": "{artist}/{album}/{song_name}.{ext}"
}
```

### 2. Spotify Credentials einrichten

Zotify benötigt Spotify-Anmeldedaten. Diese werden im `/app/credentials/` Verzeichnis gespeichert:

**Option A: Credentials JSON** (`/app/credentials/spotify-credentials.json`)

```json
{
  "USERNAME": "dein_spotify_benutzername",
  "PASSWORD": "dein_spotify_passwort",
  "AUTH_METHOD": "credentials"
}
```

**Option B: Premium Account mit Direct Decryption**

Wenn du ein Spotify Premium Account hast, können die Credentials auch verschlüsselt gespeichert werden.

### 3. Docker Compose Konfiguration

```yaml
services:
  ytdlp-api:
    build: .
    ports:
      - "8080:8080"
    volumes:
      - ./downloads:/app/downloads
      - ./configs:/app/configs
      - ./credentials:/app/credentials
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

## API-Verwendung

Die API bleibt identisch. Du verwendest den gleichen Endpoint für beide YouTube und Spotify:

### Spotify Track downloaden

```bash
curl -X POST http://localhost:8080/api/downloads/download \
  -H "Content-Type: application/json" \
  -d '"https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp"' \
  -G --data-urlencode "confName=spotify-default"
```

### Spotify Playlist downloaden

```bash
curl -X POST http://localhost:8080/api/downloads/download \
  -H "Content-Type: application/json" \
  -d '"https://open.spotify.com/playlist/37i9dQZF1DX4o1sPnc8xWl"' \
  -G --data-urlencode "confName=spotify-default"
```

### YouTube Video downloaden (wie bisher)

```bash
curl -X POST http://localhost:8080/api/downloads/download \
  -H "Content-Type: application/json" \
  -d '"https://www.youtube.com/watch?v=dQw4w9WgXcQ"' \
  -G --data-urlencode "confName=default"
```

## Dateistruktur

Nach der Integration ist die Dateistruktur wie folgt:

```
/app/
├── downloads/          # Heruntergeladene Inhalte
│   ├── music/         # Spotify Musik (nach OUTPUT-Format)
│   └── videos/        # YouTube Videos
├── configs/           # Konfigurationsdateien
│   ├── default.conf          # yt-dlp Config
│   └── spotify-default.json  # Zotify Config
├── credentials/       # Anmeldedaten
│   ├── cookies.txt           # yt-dlp Cookies
│   └── spotify-credentials.json  # Spotify Anmeldedaten
└── archive/           # Download-Archiv (optional)
```

## Zotify Konfigurationsoptionen

Hier sind die wichtigsten Zotify Konfigurationsparameter:

| Parameter | Typ | Standard | Beschreibung |
|-----------|-----|----------|-------------|
| `ROOT_PATH` | String | `/app/downloads` | Hauptdownload-Verzeichnis |
| `DOWNLOAD_FORMAT` | String | `ogg` | Audio-Format: `mp3`, `ogg`, `m4a`, `flac`, etc. |
| `DOWNLOAD_QUALITY` | String | `high` | Qualität: `normal`, `high`, `very_high` |
| `SKIP_EXISTING_FILES` | Boolean | `true` | Existierende Dateien überspringen |
| `SKIP_PREVIOUSLY_DOWNLOADED` | Boolean | `false` | Mit Archive-Datei arbeiten |
| `DOWNLOAD_REAL_TIME` | Boolean | `false` | Mit Streaming-Geschwindigkeit downloaden |
| `OUTPUT` | String | `{artist}/{album}/{song_name}.{ext}` | Ausgabepfad-Format |
| `DOWNLOAD_LYRICS` | Boolean | `true` | Lyrics herunterladen (.lrc) |
| `TRANSCODE_BITRATE` | String | `auto` | Bitrate für Transcoding |

## Architektur-Details

### URL-Erkennung

Die URL-Erkennung erfolgt in `DownloadingService.cs` durch die `IsSpotifyUrl()`-Methode:

```csharp
private static bool IsSpotifyUrl(string url)
{
    return !string.IsNullOrWhiteSpace(url) && 
           (url.Contains("spotify.com", StringComparison.OrdinalIgnoreCase) || 
            url.Contains("open.spotify.com", StringComparison.OrdinalIgnoreCase));
}
```

### Prozess-Routing

Basierend auf der URL-Erkennung wird der richtige Prozess aufgerufen:

- Spotify → `zotify` Kommand
- Alles andere → `yt-dlp` Kommand

### Konfigurationsauflösung

Die `ConfigsServices.cs` wurde erweitert, um beide `.conf` (yt-dlp) und `.json` (Zotify) Dateien zu unterstützen:

```csharp
public string GetWholeConfigPath(string configName)
{
    // Unterstützt .conf und .json
    if (configName.EndsWith(".json") || configName.EndsWith(".conf"))
    {
        return Path.Combine(configFolder, configName);
    }
    
    // Standard: .conf für yt-dlp
    return Path.Combine(configFolder, $"{configName}.conf");
}
```

## Sicherheit & Best Practices

### Spotify Credentials

⚠️ **Wichtig:** Verwende für Spotify **kein persönliches Account Passwort**. Zotify empfiehlt:

1. **Burner Account:** Erstelle einen separaten Spotify-Account für Downloads
2. **Umgebungsvariablen:** Speichere Credentials nicht im Git-Repository
3. **Volume Mounts:** Verwende Docker Secrets oder gezielt gemountete Dateien

### Download-Geschwindigkeit

- Setze `DOWNLOAD_REAL_TIME: true` für unauffällige Downloads
- Dies verhindert Account-Sperrungen durch Spotify
- Ist nur für Premium Accounts relevant

## Troubleshooting

### Zotify findet Config nicht

```
Config 'spotify-default' not found
```

**Lösung:** Stelle sicher, dass die Datei `/app/configs/spotify-default.json` existiert.

### Spotify Authentifizierung fehlgeschlagen

```
Failed to authenticate with Spotify
```

**Lösungen:**
- Überprüfe Benutzername/Passwort in den Credentials
- Verwende einen Burner Account
- Aktiviere Zwei-Faktor-Authentifizierung auf dem Spotify-Account

### Zotify Prozess stürzt ab

- Überprüfe die Docker-Logs: `docker logs <container-id>`
- Stelle sicher, dass Zotify installiert ist: `zotify --help`
- Überprüfe die Dateiberechtigungen im `/app/credentials/` Verzeichnis

## Changelog

### v1.0.0 (Zotify Integration)

- ✅ Automatische URL-Erkennung (Spotify vs. yt-dlp)
- ✅ Zotify-Installation im Docker-Image
- ✅ Multi-Format Config-Unterstützung (.conf und .json)
- ✅ Unified Credentials-Management
- ✅ Keine API-Änderungen erforderlich
- ✅ Vollständiges Logging und Error-Handling

## Weitere Ressourcen

- [Zotify GitHub Repository](https://github.com/zotify-dev/zotify)
- [Zotify Konfigurationsoptionen](https://github.com/zotify-dev/zotify/blob/master/README.md)
- [yt-dlp Dokumentation](https://github.com/yt-dlp/yt-dlp)
