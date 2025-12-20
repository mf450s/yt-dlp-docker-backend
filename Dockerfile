FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache \
    ffmpeg \
    curl \
    unzip \
    tini \
    && curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp \
    && curl -fsSL https://deno.land/x/install/install.sh | sh \
    && mv /root/.deno/bin/deno /usr/local/bin/ \
    && chmod +x /usr/local/bin/deno \
    && addgroup -g 1000 media \
    && adduser -D -u 1000 -G media yt-dlp \
    && mkdir -p /app/downloads /app/archive /app/configs \
    && chown -R yt-dlp:media /app \
    && chmod -R 775 /app

COPY --from=build /app/publish .

VOLUME ["/app/downloads", "/app/archive", "/app/configs"]
EXPOSE 8080
ENTRYPOINT ["/sbin/tini", "--"]
CMD ["dotnet", "ytdlp.Api.dll"]
