using System;
using System.Diagnostics;
using AMWin_RichPresence;
using DiscordRPC;
using DiscordRPC.Logging;

internal class AppleMusicDiscordClient {
    DiscordRpcClient? client;
    string discordClientID;
    bool enabled = false;
    public AppleMusicDiscordClient(string discordClientID, bool enabled = true) {
        this.discordClientID = discordClientID;
        this.enabled = enabled;

        if (enabled) {
            InitClient();
        }
    }

    public void SetPresence() {
        SetPresence(AppleMusicInfo.NoSong());
    }
    public void SetPresence(AppleMusicInfo amInfo) {
        if (!enabled) {
            return;
        }
        var rp = new RichPresence() {
            Details = $"{amInfo.SongName}",
            State = $"{amInfo.SongSubTitle}",
            Assets = new Assets() {
                LargeImageKey = Constants.DiscordLargeImageKey,
                SmallImageKey = Constants.DiscordSmallImageKey
            }
        };

        if (!amInfo.IsPaused) {
            rp = rp.WithTimestamps(new Timestamps(amInfo.PlaybackStart, amInfo.PlaybackEnd));
        }

        client?.SetPresence(rp);

        Trace.WriteLine($"Set Discord RP to:\n{amInfo}");
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
        client = new DiscordRpcClient(discordClientID, logger : new ConsoleLogger());
        client.Initialize();
    }
    private void DeinitClient() {
        if (client != null) {
            client.Deinitialize();
            client = null;
        }
    }
}