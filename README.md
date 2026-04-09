# YouTube Playlist to MP3 Bot

A Telegram bot for downloading audio from YouTube playlists and individual videos as MP3 files. Try the hosted version: [@YTPlaylist_to_mp3_Bot](https://t.me/YTPlaylist_to_mp3_Bot)

Made in 2024.

## Features

- **Playlist support** — send a YouTube playlist link and the bot will download all videos as MP3 files and send them one by one
- **Single video support** — works with individual YouTube video links too, not just playlists
- **Rich audio metadata** — each file is sent with the video title, channel name as performer, and album art thumbnail
- **Configurable audio quality** — choose bitrate (64–320 kbps), sample rate (22,050 / 44,100 / 48,000 Hz), and channels (mono / stereo) per user
- **Download confirmation** — before downloading a playlist, the bot shows the title and number of videos and asks for confirmation
- **Large file protection** — files over 100 MB are automatically skipped before downloading to save time
- **Bilingual interface** — full English and Ukrainian support, switchable at any time
- **Per-user settings** — each user has their own language and audio quality preferences saved independently
- **Crash-resistant** — exceptions are caught and logged without stopping the bot

## Requirements

- .NET 8
- FFmpeg

## Setup

1. Clone the repository:
```bash
git clone https://github.com/Marshmalllows/YouTube-playlist-to-mp3-bot.git
```

2. Create a `.env` file inside the `YouTube playlist to mp3 bot` folder:
```
BOT_TOKEN=your_telegram_bot_token
RESERVE_CHAT_ID=your_chat_id
```

3. Run:
```bash
cd "YouTube playlist to mp3 bot"
dotnet run -c Release
```

## Commands

| Command | Description |
|---|---|
| `/start` | Start the bot and see the welcome message |
| `/settings` | Open audio quality settings (bitrate, sample rate, channels) |
| `/change_language` | Switch between English and Ukrainian |
| `/help` | View all available commands |
| `/info` | About the project and author |

## Audio Quality Settings

| Setting | Options |
|---|---|
| Bitrate | 64 / 128 / 192 / 256 / 320 kbps |
| Sample Rate | 22,050 / 44,100 / 48,000 Hz |
| Channels | Mono / Stereo |

Default: 192 kbps, 44,100 Hz, Stereo.

## Deployment on a Server

### Fresh setup (Ubuntu 22.04)

Install dependencies, clone the repo, build and register as a systemd service:

```bash
sudo apt update && sudo apt install -y dotnet-sdk-8.0 ffmpeg git
git clone https://github.com/Marshmalllows/YouTube-playlist-to-mp3-bot.git
cd "YouTube-playlist-to-mp3-bot/YouTube playlist to mp3 bot"
echo "BOT_TOKEN=your_token" > .env
echo "RESERVE_CHAT_ID=your_chat_id" >> .env
dotnet publish -c Release -o ~/app
```

Then create `/etc/systemd/system/ytbot.service` and enable it.

### Updating

```bash
cd ~/YouTube-playlist-to-mp3-bot
git pull
cd "YouTube playlist to mp3 bot"
dotnet publish -c Release -o ~/app
sudo systemctl restart ytbot
```

## Tech Stack

- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)
- [Xabe.FFmpeg](https://github.com/tomaszzmuda/Xabe.FFmpeg)

## Author

[Maksym Poliukhovych](https://github.com/Marshmalllows)
