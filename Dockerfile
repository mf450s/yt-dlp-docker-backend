FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /src

# Copy solution and project files for dependency resolution
COPY ytdlp.sln .
COPY ytdlp.Api/ytdlp.Api.csproj ./ytdlp.Api/
COPY ytdlp.Services/ytdlp.Services.csproj ./ytdlp.Services/
COPY ytdlp.Tests/ytdlp.Tests.csproj ./ytdlp.Tests/

RUN dotnet restore ytdlp.Api/ytdlp.Api.csproj

# Copy entire source
COPY . .

# Publish without RID
RUN dotnet publish ytdlp.Api/ytdlp.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:DebugType=none \
    -p:DebugSymbols=false

# ============================================================================
# Stage 2: RUNTIME
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Set working directory
WORKDIR /app

# Install runtime dependencies in one layer (minimal image)
RUN apk add --no-cache \
    python3 \
    py3-pip \
    ffmpeg \
    curl \
    ca-certificates \
    tzdata \
    tini \
    git

# Install yt-dlp with version pinning
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp \
    -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp \
    && yt-dlp --version

# Install Zotify from PyPI
RUN pip install --no-cache-dir zotify \
    && zotify --help > /dev/null 2>&1 || true

# Create dedicated non-root user and directories
RUN addgroup -g 1000 -S media \
    && adduser -D -u 1000 -S -G media yt-dlp \
    && mkdir -p /app/downloads /app/archive /app/configs /app/cookies /app/credentials \
    && chown -R yt-dlp:media /app \
    && chmod -R 775 /app

# Copy built application from build stage
COPY --from=build --chown=yt-dlp:media /app/publish /app

# Define volumes for persistence
VOLUME ["/app/downloads", "/app/archive", "/app/configs", "/app/cookies/", "/app/credentials"]

# Switch to non-root user for security
USER yt-dlp

# Set ASP Enviroment for appsettings.json
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check only during startup - no periodic checks
HEALTHCHECK --start-period=40s \
    CMD curl -f http://localhost:8080/api/healthcheck/ready || exit 1

# Use tini as init process to handle signals correctly
ENTRYPOINT ["/sbin/tini", "--"]

# Start the application
CMD ["dotnet", "ytdlp.Api.dll"]
