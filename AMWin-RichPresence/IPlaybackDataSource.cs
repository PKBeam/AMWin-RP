using System;
using System.Threading.Tasks;

namespace AMWin_RichPresence {
    /// <summary>
    /// Reads one playback snapshot without polling, caching, or fetching metadata.
    /// A null result means the source has no accessible current track.
    /// </summary>
    internal interface IPlaybackDataSource {
        Task<PlaybackData?> GetPlaybackDataAsync();
    }

    /// <summary>
    /// Raw data from a playback source. Null playback fields mean the client did not expose them.
    /// </summary>
    internal sealed record PlaybackData(
        string SongName,
        string SongSubTitle,
        bool? IsPaused,
        double? Progress,
        string? CurrentTime,
        string? RemainingDuration,
        DateTime ObservedAt);
}
