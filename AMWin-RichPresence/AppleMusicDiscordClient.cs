using System;
using System.Diagnostics;
using System.Text;
using System.Linq;
using AMWin_RichPresence;
using DiscordRPC;

internal class AppleMusicDiscordClient {
    public enum RPSubtitleDisplayOptions {
        ArtistOnly = 0, ArtistAlbum = 1, AlbumOnly = 2
    }

    public static RPSubtitleDisplayOptions SubtitleOptionFromIndex(int i) {
        return (RPSubtitleDisplayOptions)i;
    }

    public enum RPPreviewDisplayOptions {
        Subtitle = 0, AppleMusic = 1, SongName = 2
    }

    public static RPPreviewDisplayOptions PreviewOptionFromIndex(int i) {
        return (RPPreviewDisplayOptions)i;
    }

    public RPSubtitleDisplayOptions subtitleOptions;
    public RPPreviewDisplayOptions previewOptions;
    DiscordRpcClient? client;
    string discordClientID;
    bool enabled = false;
    Logger? logger;
    int maxStringLength = 127;
    string songLyrics = string.Empty;

    public AppleMusicDiscordClient(
        string discordClientID, 
        bool enabled = true,
        RPSubtitleDisplayOptions subtitleOptions = RPSubtitleDisplayOptions.ArtistOnly,
        RPPreviewDisplayOptions previewOptions = RPPreviewDisplayOptions.Subtitle, 
        Logger? logger = null
    ) {
        this.discordClientID = discordClientID;
        this.enabled = enabled;
        this.subtitleOptions = subtitleOptions;
        this.previewOptions = previewOptions;
        this.logger = logger;

        if (enabled) {
            InitClient();
        }
    }

    private string TrimString(string str) {
        return str.Length > maxStringLength ? str.Substring(0, maxStringLength - 1) : str;
    }

    private string GetTrimmedArtistList(AppleMusicInfo amInfo) {
        if (amInfo.ArtistList?.Count > 1) {
            return $"{amInfo.ArtistList.First()}, Various Artists";
        } else {
            return amInfo.SongArtist; // TODO fix this so it always prevents string length violations
        }
    }

    public void SetPresence(AppleMusicInfo amInfo, bool showSmallImage, bool showBigImage) {
        if (!enabled) {
            return;
        }

        var songName = TrimString(amInfo.SongName);
        var songSubtitle = amInfo.SongSubTitle.Length > maxStringLength ? amInfo.SongSubTitle.Replace(amInfo.SongArtist, GetTrimmedArtistList(amInfo)) : amInfo.SongSubTitle;
        var songArtist = amInfo.SongArtist.Length > maxStringLength ? GetTrimmedArtistList(amInfo) : amInfo.SongArtist;
        var songAlbum = TrimString(amInfo.SongAlbum);

        // hack to show 1-character song names
        while (songName.Length < 2) {
            songName += "\u0000";
        }

        // pick the subtitle format to show
        var subtitle = "";
        switch (subtitleOptions) {
            case RPSubtitleDisplayOptions.ArtistAlbum:
                subtitle = songSubtitle;
                break;
            case RPSubtitleDisplayOptions.ArtistOnly:
                subtitle = songArtist;
                break;
            case RPSubtitleDisplayOptions.AlbumOnly:
                subtitle = songAlbum;
                break;
        }

        var statusDisplay = StatusDisplayType.Details;
        switch (previewOptions) {
            case RPPreviewDisplayOptions.Subtitle:
                statusDisplay = StatusDisplayType.State;
                break;
            case RPPreviewDisplayOptions.AppleMusic:
                statusDisplay = StatusDisplayType.Name;
                break;
            case RPPreviewDisplayOptions.SongName:
                statusDisplay = StatusDisplayType.Details;
                break;
        }

        if (ASCIIEncoding.Unicode.GetByteCount(subtitle) > 128) {
            // TODO fix this to account for multibyte unicode characters
            subtitle = subtitle.Substring(0, 60) + "...";
        }
        // update lyrics
        songLyrics = string.Empty;
        if (amInfo.SyncedLyrics != null && AMWin_RichPresence.Properties.Settings.Default.EnableSyncLyrics) {
            var currentTime = amInfo.CurrentTime != null ? TimeSpan.FromSeconds((int)amInfo.CurrentTime) : (DateTime.UtcNow - (amInfo.PlaybackStart ?? DateTime.UtcNow));
            songLyrics = LRCLibClient.GetCurrentLyric(amInfo.SyncedLyrics, currentTime);
        }

        try {
            var rp = new RichPresence() {
                Details = songName,
                State = subtitle,
                Assets = new Assets() {
                    LargeImageKey = (showBigImage ? amInfo.CoverArtUrl : null) ?? Constants.DiscordAppleMusicImageKey,
                    LargeImageText = !string.IsNullOrEmpty(songLyrics) ? songLyrics : null
                },
                Type = ActivityType.Listening,
                StatusDisplay = statusDisplay,
            };
            
            if (amInfo.SongUrl != null) {
                var buttonLabel = AMWin_RichPresence.Properties.Settings.Default.RPCButtonLanguage == 1 ? "Apple Music'de Dinle" : "Listen on Apple Music";
                rp.Buttons = [new() {
                    Label = buttonLabel, 
                    Url = amInfo.SongUrl
                }];
            }

            if (amInfo.IsPaused) {
                rp.Assets.SmallImageKey = Constants.DiscordAppleMusicPauseImageKey;
            } else if (showSmallImage) {
                rp.Assets.SmallImageKey = (!showBigImage || amInfo.CoverArtUrl == null) ? Constants.DiscordAppleMusicPlayImageKey : Constants.DiscordAppleMusicImageKey;
            }

            // add timestamps, if they're there
            if (!amInfo.IsPaused && amInfo.PlaybackStart != null && amInfo.PlaybackEnd != null) {
                rp = rp.WithTimestamps(new Timestamps((DateTime)amInfo.PlaybackStart, (DateTime)amInfo.PlaybackEnd));
            }

            if (client == null) {
                logger?.Log($"Tried to set Discord RP, but no client");
            } else {
                client.SetPresence(rp);
                logger?.Log($"Set Discord RP to:\n{amInfo}");
            }

        } catch (Exception ex) {
            logger?.Log($"Couldn't set Discord RP:\n{ex}");
        }

    }
    public void Enable() {
        if (enabled) {
            return;
        }
        enabled = true;
        InitClient();
    }
    public void Disable() {
        if (!enabled) {
            return;
        }
        enabled = false;
        client?.ClearPresence();
        DeinitClient();
    }
    private void InitClient() {
        client = new DiscordRpcClient(discordClientID, logger: logger);
        client.Initialize();
    }
    private void DeinitClient() {
        if (client != null) {
            client.Deinitialize();
            client.Dispose();
            client = null;
        }
    }
}
