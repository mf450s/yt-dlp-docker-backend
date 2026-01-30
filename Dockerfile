# ============================================================================
# Stage 1: BUILD
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ytdlp.sln .
COPY ytdlp.Api/ytdlp.Api.csproj ./ytdlp.Api/
COPY ytdlp.Services/ytdlp.Services.csproj ./ytdlp.Services/
COPY ytdlp.Tests/ytdlp.Tests.csproj ./ytdlp.Tests/

RUN dotnet restore ytdlp.Api/ytdlp.Api.csproj
COPY . .

RUN dotnet publish ytdlp.Api/ytdlp.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:DebugType=none \
    -p:DebugSymbols=false

# ============================================================================
# Stage 2: RUNTIME (Debian-based, easier for deno)
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg \
    curl \
    ca-certificates \
    tzdata \
    tini \
    git \
    python3 \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

# yt-dlp
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp \
    -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp \
    && yt-dlp --version

# deno (JS runtime for YouTube)
RUN curl -fsSL https://deno.land/install.sh | sh \
    && ln -s /root/.deno/bin/deno /usr/local/bin/deno \
    && deno --version

# zotify
RUN pip3 install --no-cache-dir zotify \
    && zotify --help > /dev/null 2>&1 || true

RUN groupadd -g 1000 media \
    && useradd -u 1000 -g media -m yt-dlp \
    && mkdir -p /app/downloads /app/archive /app/configs /app/cookies /app/credentials \
    && chown -R yt-dlp:media /app \
    && chmod -R 775 /app

COPY --from=build --chown=yt-dlp:media /app/publish /app

VOLUME ["/app/downloads", "/app/archive", "/app/configs", "/app/cookies", "/app/credentials"]

USER yt-dlp
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

HEALTHCHECK --start-period=40s \
    CMD curl -f http://localhost:8080/api/healthcheck/ready || exit 1

ENTRYPOINT ["/usr/bin/tini", "--"]
CMD ["dotnet", "ytdlp.Api.dll"]
