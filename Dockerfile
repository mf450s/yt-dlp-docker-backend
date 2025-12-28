FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ytdlp.sln .
COPY ytdlp.Api/*.csproj ./ytdlp.Api/
COPY ytdlp.Services/*.csproj ./ytdlp.Services/
COPY ytdlp.Tests/*.csproj ./ytdlp.Tests/
RUN dotnet restore

COPY . .
RUN dotnet build -c Release --no-restore
RUN dotnet publish ytdlp.Api/ytdlp.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache \
    python3 \
    py3-pip \
    ffmpeg \
    curl \
    tini \
    && python3 -m pip install --break-system-packages -U yt-dlp \
    && curl -fsSL https://deno.land/x/install/install.sh | sh \
    && mv /root/.deno/bin/deno /usr/local/bin/ \
    && chmod +x /usr/local/bin/deno \
    && addgroup -g 1000 media \
    && adduser -D -u 1000 -G media yt-dlp \
    && mkdir -p /app/downloads /app/archive /app/configs

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

USER yt-dlp
EXPOSE 8080
ENTRYPOINT ["/sbin/tini", "--"]
CMD ["dotnet", "ytdlp.Api.dll"]
