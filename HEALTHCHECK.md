# Health Check API

Dieses Dokument beschreibt die Health-Check Endpoints der yt-dlp API.

## Übersicht

Die API bietet drei Health-Check Endpoints für unterschiedliche Use-Cases:

1. **Detailed Health** (`/api/healthcheck/detailed`) - Vollständige Diagnose
2. **Liveness Probe** (`/api/healthcheck/live`) - Prozess läuft noch
3. **Readiness Probe** (`/api/healthcheck/ready`) - API bereit für Requests

## Endpoints

### 1. Detailed Health Check

```bash
GET /api/healthcheck/detailed
```

**Beschreibung:** Führt umfassende Diagnose durch und liefert detaillierte Informationen über den Status aller Systemkomponenten.

**Response (Healthy - 200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2025-12-29T18:00:00.000Z",
  "details": {
    "ytdlp_available": true,
    "download_dir_writable": true,
    "response_time_ms": 42,
    "timestamp": "2025-12-29T18:00:00.000Z"
  }
}
```

**Response (Unhealthy - 503 Service Unavailable):**
```json
{
  "status": "Unhealthy",
  "timestamp": "2025-12-29T18:00:00.000Z",
  "details": {
    "ytdlp_available": false,
    "download_dir_writable": true,
    "response_time_ms": 125,
    "timestamp": "2025-12-29T18:00:00.000Z"
  }
}
```

**Status Codes:**
- `200 OK` - API ist healthy
- `503 Service Unavailable` - API ist unhealthy
- `408 Request Timeout` - Health-Check Timeout (bei sehr langen Checks)

**Diagnose-Details:**
- `ytdlp_available`: Prüft, ob yt-dlp installiert und erreichbar ist
- `download_dir_writable`: Prüft, ob das Download-Verzeichnis beschreibbar ist
- `response_time_ms`: Antwortzeit des Health-Checks in Millisekunden

### 2. Liveness Probe

```bash
GET /api/healthcheck/live
```

**Beschreibung:** Leichte Probe für Orchestrierungsplattformen (Docker, Kubernetes). Prüft nur, ob der Prozess noch läuft.

**Response (200 OK):**
```json
{
  "status": "alive",
  "timestamp": "2025-12-29T18:00:00.000Z"
}
```

**Status Codes:**
- `200 OK` - Prozess läuft

**Verwendung:**
- Docker: `healthcheck`-Direktive
- Kubernetes: `livenessProbe`

### 3. Readiness Probe

```bash
GET /api/healthcheck/ready
```

**Beschreibung:** Prüft, ob die API bereit ist, Downloads zu verarbeiten. Führt vollständige Diagnose durch.

**Response (Ready - 200 OK):**
```json
{}
```

**Response (Not Ready - 503 Service Unavailable):**
```json
{}
```

**Status Codes:**
- `200 OK` - API ist ready und kann Requests verarbeiten
- `503 Service Unavailable` - API ist not ready

**Verwendung:**
- Kubernetes: `readinessProbe`
- Load Balancer: Routing-Entscheidungen

## Docker Integration

Der Health-Check ist bereits in der `docker-compose.yml` konfiguriert:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/api/healthcheck/ready"]
  interval: 30s
  timeout: 5s
  retries: 3
  start_period: 10s
```

**Konfiguration:**
- **interval**: Health-Check alle 30 Sekunden
- **timeout**: 5 Sekunden Timeout pro Check
- **retries**: Container wird als unhealthy nach 3 fehlgeschlagenen Checks markiert
- **start_period**: 10 Sekunden Startverzögerung

## Kubernetes Integration

### Liveness Probe
```yaml
livenessProbe:
  httpGet:
    path: /api/healthcheck/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 30
  timeoutSeconds: 5
  failureThreshold: 3
```

### Readiness Probe
```yaml
readinessProbe:
  httpGet:
    path: /api/healthcheck/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

## Beispiele

### cURL
```bash
# Detailed Health
curl -v http://localhost:8080/api/healthcheck/detailed

# Liveness
curl -v http://localhost:8080/api/healthcheck/live

# Readiness
curl -v http://localhost:8080/api/healthcheck/ready
```

### PowerShell
```powershell
# Detailed Health
Invoke-WebRequest -Uri "http://localhost:8080/api/healthcheck/detailed"

# Readiness
Invoke-WebRequest -Uri "http://localhost:8080/api/healthcheck/ready"
```

### JavaScript
```javascript
// Detailed Health
fetch('http://localhost:8080/api/healthcheck/detailed')
  .then(r => r.json())
  .then(data => console.log(data))
  .catch(e => console.error(e));

// Readiness
fetch('http://localhost:8080/api/healthcheck/ready')
  .then(r => r.status === 200 ? 'Ready' : 'Not Ready')
  .then(status => console.log(status))
  .catch(e => console.error(e));
```

## Performance

**Typical Response Times:**
- Liveness Probe: < 1ms
- Readiness Probe: 50-200ms (abhängig von yt-dlp Verfügbarkeit)
- Detailed Health: 50-200ms

**Timeout-Werte:**
- yt-dlp Availability Check: 5 Sekunden
- Download Dir Check: < 1 Sekunde
- Gesamter Health-Check: < 10 Sekunden

## Logging

Alle Health-Checks werden geloggt:

```
INFO: Health check completed with status: Healthy
INFO: Health check request completed. Status: Healthy, ResponseTime: 42ms
```

Bei Fehlern:
```
WARNING: yt-dlp is not available or not accessible
ERROR: Health check failed: Exception details...
```

## Troubleshooting

### "ytdlp_available": false

**Problem:** yt-dlp ist nicht installiert oder nicht im PATH.

**Lösung:**
```bash
# Im Docker Container
which yt-dlp
yt-dlp --version

# yt-dlp installieren
pip install yt-dlp
```

### "download_dir_writable": false

**Problem:** Das Download-Verzeichnis ist nicht beschreibbar.

**Lösung:**
```bash
# Berechtigungen prüfen
ls -la /downloads

# Berechtigungen setzen
chmod 755 /downloads
chown app:app /downloads  # falls app-user existiert
```

### Health-Check Timeout

**Problem:** Die Readiness-Probe dauert länger als das Timeout.

**Lösung:**
- Timeout-Wert in docker-compose.yml oder Kubernetes erhöhen
- yt-dlp Installation überprüfen
- Netzwerk-Verbindung überprüfen (falls yt-dlp Updates benötigt)

## Best Practices

1. **Liveness** für Prozessüberwachung verwenden
2. **Readiness** vor dem Routing zu Container verwenden
3. **Detailed** für Debugging und Monitoring
4. Health-Checks in Orchestrierungsplattform konfigurieren
5. Alerts für wiederholte "Unhealthy" Status setzen
6. Monitoring-Dashboard mit `/api/healthcheck/detailed` aufbauen
