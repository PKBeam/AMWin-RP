using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace AMWin_RichPresence {
    internal enum PlaybackSourceType {
        Client = 0,
        WindowsRuntime = 1
    }

    /// <summary>
    /// Reads Apple Music's system media session independently of its windows.
    /// </summary>
    internal sealed class AppleMusicWindowsRuntimeSource : IPlaybackDataSource {
        private const string AppleMusicAppId = "AppleInc.AppleMusicWin_nzyj5cx40ttqa!App";
        private readonly Logger? logger;

        public AppleMusicWindowsRuntimeSource(Logger? logger = null) {
            this.logger = logger;
        }

        public async Task<PlaybackData?> GetPlaybackDataAsync() {
            try {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                // The current system session may belong to another player.
                var session = manager.GetSessions().FirstOrDefault(s => s.SourceAppUserModelId == AppleMusicAppId);
                if (session == null) return null;

                var properties = await session.TryGetMediaPropertiesAsync();
                if (properties == null || string.IsNullOrWhiteSpace(properties.Title)
                    || properties.Title.EndsWith("'s Station", StringComparison.Ordinal)) return null;

                var playback = session.GetPlaybackInfo();
                if (playback == null) return null;
                var status = playback.PlaybackStatus;
                if (status is GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed
                    or GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped) return null;

                bool? isPaused = status switch {
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => false,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => true,
                    _ => null
                };

                var timeline = session.GetTimelineProperties();
                var observedAt = DateTime.UtcNow;
                (string? CurrentTime, string? RemainingDuration, double? Progress) timing = timeline == null ? (null, null, null) : GetTiming(
                    timeline.StartTime, timeline.EndTime, timeline.Position, timeline.LastUpdatedTime,
                    isPaused == false, playback.PlaybackRate, observedAt);

                return new PlaybackData(properties.Title, GetSongSubtitle(properties.Artist, properties.AlbumTitle),
                    isPaused, timing.Progress, timing.CurrentTime, timing.RemainingDuration, observedAt);
            } catch (Exception ex) {
                // Sessions can disappear between enumeration and reading their properties.
                logger?.Log($"Could not read Apple Music Windows Runtime session: {ex}");
                return null;
            }
        }

        internal static string GetSongSubtitle(string artist, string album) {
            // Apple Music uses the format "artist — album" in Artist while leaving AlbumTitle.
            if (string.IsNullOrWhiteSpace(album) || artist.Contains(" — ", StringComparison.Ordinal)) return artist;
            return $"{artist} — {album}";
        }

        internal static (string? CurrentTime, string? RemainingDuration, double? Progress) GetTiming(
            TimeSpan start, TimeSpan end, TimeSpan position, DateTimeOffset lastUpdatedTime,
            bool isPlaying, double? playbackRate, DateTime observedAt) {
            var duration = (end - start).TotalSeconds;
            // An empty timeline must not overwrite duration obtained from web metadata.
            if (duration <= 0) return (null, null, null);

            var current = (position - start).TotalSeconds;
            // Position is only current as of LastUpdatedTime, which need not advance on each poll.
            if (isPlaying && lastUpdatedTime > DateTimeOffset.UnixEpoch) {
                var elapsed = Math.Max(0, (new DateTimeOffset(observedAt) - lastUpdatedTime).TotalSeconds);
                var rate = playbackRate ?? 1;
                if (double.IsFinite(rate) && rate >= 0) current += elapsed * rate;
            }
            current = Math.Clamp(current, 0, duration);
            var currentSeconds = (long)current;
            var remainingSeconds = (long)duration - currentSeconds;
            return (FormatTime(currentSeconds), FormatTime(remainingSeconds), current / duration);
        }

        private static string FormatTime(long seconds) {
            return string.Create(CultureInfo.InvariantCulture, $"{seconds / 60}:{seconds % 60:00}");
        }
    }
}
