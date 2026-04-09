FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "YouTube playlist to mp3 bot/YouTube playlist to mp3 bot.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "YouTube playlist to mp3 bot.dll"]
