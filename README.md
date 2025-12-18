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
"DownloadDir": "../downloads",
"ArchiveDir": "../archives",
"ConfigDir": "../configs"
```



---
Dump:


ich rufe yt-dlp als binary auf(glaub ich). Ich konfiguriere halt nen yt-dlp befehl. yt-dlp und seine dependencys sollen mitinstalliert werden. ffmpeg zum beispiel auch. Und ich bekomme irgendne jsruntime warnung und irgendwas von "Deno" steht da dann auch bei. Das sollte gefixt werden


  "Dirs": {
    "DownloadDir": "../downloads",
    "ArchiveDir": "../archives",
    "ConfigDir": "../configs"
  }

  - [ ] Command Injektion fixen