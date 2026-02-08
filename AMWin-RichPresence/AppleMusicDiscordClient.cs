using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AMWin_RichPresence;
using DiscordRPC;
using Localisation = AMWin_RichPresence.Properties.Localisation;

internal class AppleMusicDiscordClient {
    public enum RPStatusDisplayOptions {
        Artist = 0, AppleMusic = 1, SongName = 2
     }

    public static RPStatusDisplayOptions StatusDisplayOptionFromIndex(int i) {
        return (RPStatusDisplayOptions)i;
    }

    public RPStatusDisplayOptions statusDisplayOptions;
    DiscordRpcClient? client;
    string discordClientID;
    bool enabled = false;
    Logger? logger;
    int maxStringLength = 127;
    string? songLyrics = null;

    public AppleMusicDiscordClient(
        string discordClientID, 
        bool enabled = true,
        RPStatusDisplayOptions statusDisplayOptions = RPStatusDisplayOptions.Artist, 
        Logger? logger = null
    ) {
        this.discordClientID = discordClientID;
        this.enabled = enabled;
        this.statusDisplayOptions = statusDisplayOptions;
        this.logger = logger;

        if (enabled) {
            InitClient();
        }
    }

    private string TrimString(string str, uint maxLength = 127) {
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
        var statusDisplay = StatusDisplayType.Details;
        switch (statusDisplayOptions) {
            case RPStatusDisplayOptions.Artist:
                statusDisplay = StatusDisplayType.State;
                break;
            case RPStatusDisplayOptions.AppleMusic:
                statusDisplay = StatusDisplayType.Name;
                break;
            case RPStatusDisplayOptions.SongName:
                statusDisplay = StatusDisplayType.Details;
                break;
        }

        if (ASCIIEncoding.Unicode.GetByteCount(songArtist) > 128) {
            // TODO fix this to account for multibyte unicode characters
            songArtist = songArtist.Substring(0, 60) + "...";
        }
        // update lyrics
        if (AMWin_RichPresence.Properties.Settings.Default.EnableSyncLyrics && amInfo.SyncedLyrics != null) {
            var currentTime = amInfo.CurrentTime != null ? TimeSpan.FromSeconds((int)amInfo.CurrentTime) : (DateTime.UtcNow - (amInfo.PlaybackStart ?? DateTime.UtcNow));
            songLyrics = LRCLibClient.GetCurrentLyric(amInfo.SyncedLyrics, currentTime);
        } else {
            songLyrics = null;
        }

        try {
            var rp = new RichPresence() {
                Details = songName,
                State = songArtist,
                Assets = new Assets() {
                    LargeImageKey = (showBigImage ? amInfo.CoverArtUrl : null) ?? Constants.DiscordAppleMusicImageKey ?? "",
                    LargeImageText = songLyrics ?? songAlbum,
                    SmallImageKey = "",
                    SmallImageText = ""
                },
                Type = ActivityType.Listening,
                StatusDisplay = statusDisplay,
            };

            var buttons = new List<Button>();

            if (amInfo.SongUrl != null) {
                buttons.Add(new Button() {
                    Label = Localisation.DiscordButton_ListenOnAppleMusic,
                    Url = amInfo.SongUrl
                });
            }

            if (amInfo.ArtistUrl != null) {
                buttons.Add(new Button() {
                    Label = Localisation.DiscordButton_ViewArtist,
                    Url = amInfo.ArtistUrl
                });
            }

            if (buttons.Count > 0) {
                rp.Buttons = buttons.ToArray();
            }

            if (amInfo.IsPaused) {
                rp.Assets.SmallImageKey = Constants.DiscordAppleMusicPauseImageKey ?? "";
            } else if (showSmallImage) {
                rp.Assets.SmallImageKey = ((!showBigImage || amInfo.CoverArtUrl == null) ? Constants.DiscordAppleMusicPlayImageKey : Constants.DiscordAppleMusicImageKey) ?? "";
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
