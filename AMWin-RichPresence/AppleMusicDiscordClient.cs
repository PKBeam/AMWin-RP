using System.Diagnostics;
using AMWin_RichPresence;
using DiscordRPC;

internal class AppleMusicDiscordClient {
    public enum RPSubtitleDisplayOptions {
        ArtistAlbum = 0, ArtistOnly = 1, AlbumOnly = 2
    }

    public static RPSubtitleDisplayOptions SubtitleOptionFromIndex(int i) {
        return (RPSubtitleDisplayOptions)i;
    }

    public RPSubtitleDisplayOptions subtitleOptions;
    DiscordRpcClient? client;
    string discordClientID;
    bool enabled = false;

    public AppleMusicDiscordClient(string discordClientID, bool enabled = true, RPSubtitleDisplayOptions subtitleOptions = RPSubtitleDisplayOptions.ArtistAlbum) {
        this.discordClientID = discordClientID;
        this.enabled = enabled;
        this.subtitleOptions = subtitleOptions;

        if (enabled) {
            InitClient();
        }
    }

    public void SetPresence(AppleMusicInfo amInfo, bool showSmallImage) {
        if (!enabled || !amInfo.HasSong) {
            return;
        }

        // pick the subtitle format to show
        var subtitle = "";
        switch (subtitleOptions) {
            case RPSubtitleDisplayOptions.ArtistAlbum:
                subtitle = $"{amInfo.SongSubTitle}";
                break;
            case RPSubtitleDisplayOptions.ArtistOnly:
                subtitle = $"{amInfo.SongArtist}";
                break;
            case RPSubtitleDisplayOptions.AlbumOnly:
                subtitle = $"{amInfo.SongAlbum}";
                break;
        }

        var rp = new RichPresence() {
            Details = $"{amInfo.SongName}",
            State = subtitle,
            Assets = new Assets() {
                LargeImageKey = amInfo.CoverArtUrl ?? Constants.DiscordAppleMusicImageKey,
                LargeImageText = amInfo.SongSubTitle
            }
        };

        if (showSmallImage) {
            rp.Assets.SmallImageKey = (amInfo.CoverArtUrl == null) ? Constants.DiscordAppleMusicPlayImageKey : Constants.DiscordAppleMusicImageKey;
        }

        if (!amInfo.IsPaused) {
            rp = rp.WithTimestamps(new Timestamps(amInfo.PlaybackStart, amInfo.PlaybackEnd));
        }

        client?.SetPresence(rp);

        Trace.WriteLine($"Set Discord RP to:\n{amInfo}\n");
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
        client = new DiscordRpcClient(discordClientID);
        client.Initialize();
    }
    private void DeinitClient() {
        if (client != null) {
            client.Deinitialize();
            client = null;
        }
    }
}