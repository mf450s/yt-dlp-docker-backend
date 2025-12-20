FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache \
    python3 \
    py3-pip \
    ffmpeg \
    curl \
    unzip \
    tini \
    && pip install --no-cache-dir --upgrade --break-system-packages yt-dlp \
    && curl -fsSL https://deno.land/x/install/install.sh | sh \
    && mv /root/.deno/bin/deno /usr/local/bin/ \
    && chmod +x /usr/local/bin/deno \
    && addgroup -g 1000 media \
    && adduser -D -u 1000 -G media yt-dlp \
    && mkdir -p /app/downloads /app/archive /app/configs \
    && chown -R yt-dlp:media /app \
    && chmod -R 775 /app

COPY --from=build /app/publish .

RUN echo "--restrict-filenames" > /app/configs/default.conf \
    && echo "--embed-thumbnail" >> /app/configs/default.conf \
    && echo "--embed-metadata" >> /app/configs/default.conf \
    && echo "--sponsorblock-mark selfpromo,intro,outro,hook" >> /app/configs/default.conf

VOLUME ["/app/downloads", "/app/archive", "/app/configs"]
EXPOSE 8080

ENTRYPOINT ["/sbin/tini", "--"]
CMD ["dotnet", "YourApi.dll"]
