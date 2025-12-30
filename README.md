Standard Api Endpoints:

Post: `/api/ytdlp/download?confName={name}`
Body: `videolink`
Returns: `202`
Downloads the Video

Get: `/api/ytdlp/config/`
Returns: List of Names of config files

Get, Patch, Post, Delete, : `/api/ytdlp/config/{configName}`
Get, edit content of config; Add new or delete config

Standard Directories:

```
"Downloads": "/app/downloads",
"Archive": "/app/archive",
"Config": "/app/config"
```



---
## ToDO
- [ ] Command Injektion fixen
- [ ] cookie text support
- [ ] 

---
Dump:


ich rufe yt-dlp als binary auf(glaub ich). Ich konfiguriere halt nen yt-dlp befehl. yt-dlp und seine dependencys sollen mitinstalliert werden. ffmpeg zum beispiel auch. Und ich bekomme irgendne jsruntime warnung und irgendwas von "Deno" steht da dann auch bei. Das sollte gefixt werden
