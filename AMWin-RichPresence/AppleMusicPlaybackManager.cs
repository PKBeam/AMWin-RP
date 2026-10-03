using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace AMWin_RichPresence {
    /// <summary>
    /// Polls a playback source and maintains the enriched current track for consumers.
    /// </summary>
    internal sealed class AppleMusicPlaybackManager : IDisposable {
        private sealed class WebReqFailCounters {
            public int MaxFails = Constants.NumFailedSearchesBeforeAbandon;

            public int SongDuration = 0;
            public int AlbumArt = 0;
            public int ArtistList = 0;
            public int SongUrl = 0;
            public int ArtistUrl = 0;

        }

        private static readonly Regex ComposerPerformerRegex = new Regex(@"By\s.*?\s\u2014", RegexOptions.Compiled);

        private readonly IPlaybackDataSource playbackSource;
        private readonly string? lastFmApiKey;
        private readonly Timer timer;
        private readonly Action<AppleMusicInfo?> refreshHandler;
        private readonly Logger? logger;
        private readonly LRCLibClient lrclibClient;
        private AppleMusicInfo? currentSong;
        private PlaybackData? previousPlayback;
        private string appleMusicRegion;
        private WebReqFailCounters webReqFails = new();
        private Task? metadataTask;
        private int refreshInProgress;
        private int invalidateMetadata;
        private volatile bool disposed;

        public bool ComposerAsArtist { get; set; }

        public AppleMusicPlaybackManager(IPlaybackDataSource playbackSource, string? lastFmApiKey,
            int refreshPeriodInSec, bool composerAsArtist, string appleMusicRegion,
            Action<AppleMusicInfo?> refreshHandler, Logger? logger = null) {
            this.playbackSource = playbackSource;
            this.lastFmApiKey = lastFmApiKey;
            this.ComposerAsArtist = composerAsArtist;
            this.appleMusicRegion = appleMusicRegion;
            this.refreshHandler = refreshHandler;
            this.logger = logger;
            lrclibClient = new LRCLibClient(logger);
            timer = new Timer(refreshPeriodInSec * 1000);
            timer.Elapsed += OnTimerElapsed;
        }

        public void Start() {
            ObjectDisposedException.ThrowIf(disposed, this);
            timer.Start();
            _ = RefreshAsync();
        }

        public void ChangeRegion(string region) {
            appleMusicRegion = region;
            Interlocked.Exchange(ref invalidateMetadata, 1);
            _ = RefreshAsync();
        }

        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e) {
            await RefreshAsync();
        }

        public async Task RefreshAsync() {
            // A slow source or callback must not create overlapping polls.
            if (disposed || Interlocked.CompareExchange(ref refreshInProgress, 1, 0) != 0) return;

            try {
                if (Interlocked.Exchange(ref invalidateMetadata, 0) != 0) {
                    currentSong = null;
                }

                try {
                    await UpdateSongAsync();
                } catch (Exception ex) {
                    logger?.Log($"Something went wrong while reading playback data: {ex}");
                }

                if (!disposed) refreshHandler(currentSong);
            } catch (Exception ex) {
                logger?.Log($"Something went wrong while refreshing playback: {ex}");
            } finally {
                Interlocked.Exchange(ref refreshInProgress, 0);
            }
        }

        private async Task UpdateSongAsync() {
            var playback = playbackSource.GetPlaybackData();
            if (playback == null) {
                currentSong = null;
                previousPlayback = null;
                metadataTask = null;
                return;
            }

            string songArtist = "";
            string songAlbum = "";
            string? songPerformer = null;
            try {
                (songArtist, songAlbum, songPerformer) = ParseSongAlbumArtist(playback.SongSubTitle, ComposerAsArtist);
            } catch (Exception ex) {
                logger?.Log($"Could not parse '{playback.SongSubTitle}' into artist and album: {ex}");
            }

            var newSong = new AppleMusicInfo(playback.SongName, playback.SongSubTitle, songAlbum, songArtist);
            if (currentSong != newSong) {
                if (newSong.SongAlbum == currentSong?.SongAlbum && newSong.SongArtist == currentSong?.SongArtist) {
                    newSong.CoverArtUrl = currentSong.CoverArtUrl;
                }
                currentSong = newSong;
                webReqFails = new();
                previousPlayback = null;
                metadataTask = null;
            }

            var song = currentSong!;
            UpdatePlaybackState(song, playback);
            UpdatePlaybackTiming(song, playback);

            if (metadataTask == null || metadataTask.IsCompleted) {
                // Capture this song and its counters: a late response must not enrich another track.
                var webScraper = new AppleMusicWebScraper(song.SongName, song.SongAlbum,
                    songPerformer ?? song.SongArtist, appleMusicRegion, logger, lastFmApiKey);
                metadataTask = EnrichSongAsync(song, webScraper, webReqFails);
            }

            // Publish playback promptly while allowing slow metadata requests to finish.
            await Task.WhenAny(metadataTask, Task.Delay(2000));
            UpdatePlaybackTiming(song, playback);
        }

        private void UpdatePlaybackState(AppleMusicInfo song, PlaybackData playback) {
            bool? isPaused = playback.IsPaused;
            if (!isPaused.HasValue) {
                if (playback.Progress.HasValue && previousPlayback?.Progress is double previousProgress) {
                    isPaused = playback.Progress.Value == previousProgress;
                } else if (ParseTimeString(playback.CurrentTime) is int currentTime
                    && ParseTimeString(previousPlayback?.CurrentTime) is int previousTime) {
                    isPaused = currentTime == previousTime;
                } else if (ParseTimeString(playback.RemainingDuration) is int remainingDuration
                    && ParseTimeString(previousPlayback?.RemainingDuration) is int previousRemainingDuration) {
                    isPaused = remainingDuration == previousRemainingDuration;
                }
            }

            // Missing controls provide no evidence of a playback state change.
            song.IsPaused = isPaused ?? song.IsPaused;
            previousPlayback = playback;
        }

        private static void UpdatePlaybackTiming(AppleMusicInfo song, PlaybackData playback) {
            var currentTime = ParseTimeString(playback.CurrentTime);
            var remainingDuration = ParseTimeString(playback.RemainingDuration);
            if (currentTime.HasValue && remainingDuration.HasValue) {
                song.SongDuration = currentTime.Value + remainingDuration.Value;
            }

            if (song.SongDuration.HasValue) {
                if (playback.Progress.HasValue) {
                    currentTime ??= (int)(playback.Progress.Value * song.SongDuration.Value);
                }
                if (currentTime.HasValue) {
                    remainingDuration ??= song.SongDuration.Value - currentTime.Value;
                }
            }

            song.CurrentTime = currentTime;
            song.PlaybackStart = currentTime.HasValue ? playback.ObservedAt.AddSeconds(-currentTime.Value) : null;
            song.PlaybackEnd = remainingDuration.HasValue ? playback.ObservedAt.AddSeconds(remainingDuration.Value) : null;
        }

        private bool IsCurrentSong(AppleMusicInfo song) {
            return !disposed && ReferenceEquals(currentSong, song);
        }

        private async Task EnrichSongAsync(AppleMusicInfo song, AppleMusicWebScraper webScraper, WebReqFailCounters counters) {
            try {
                await DoWebScrapes(song, webScraper, counters);
            } catch (Exception ex) {
                logger?.Log($"Something went wrong while fetching song metadata: {ex}");
            }
        }

        public void Dispose() {
            disposed = true;
            timer.Elapsed -= OnTimerElapsed;
            timer.Dispose();
        }

        private async Task DoWebScrapes(AppleMusicInfo song, AppleMusicWebScraper webScraper, WebReqFailCounters webReqFails) {
            if (!IsCurrentSong(song)) {
                return;
            }

            // web query for song duration if we don't have it
            if (song.SongDuration == null && webReqFails.SongDuration < webReqFails.MaxFails) {
                var result = ParseTimeString(await webScraper.GetSongDuration());
                if (!IsCurrentSong(song)) return;
                if (result == null) {
                    webReqFails.SongDuration++;
                    if (webReqFails.SongDuration == webReqFails.MaxFails) {
                        logger?.Log("Reached max fails for GetSongDuration.");
                    }
                } else {
                    webReqFails.SongDuration = 0;
                }
                song.SongDuration ??= result;
            }

            // ================================================
            //  Get song cover art
            // ------------------------------------------------

            if (song.CoverArtUrl == null && webReqFails.AlbumArt < webReqFails.MaxFails) {
                var result = await webScraper.GetAlbumArtUrl();
                if (!IsCurrentSong(song)) return;
                if (result == null) {
                    webReqFails.AlbumArt++;
                    if (webReqFails.AlbumArt == webReqFails.MaxFails) {
                        logger?.Log("Reached max fails for GetAlbumArt.");
                    }
                } else {
                    webReqFails.AlbumArt = 0;
                }
                song.CoverArtUrl = result;
            }

            // ================================================
            //  Get song artists, as a list
            // ------------------------------------------------

            if (song.ArtistList == null && webReqFails.ArtistList < webReqFails.MaxFails) {
                var result = await webScraper.GetArtistList();
                if (!IsCurrentSong(song)) return;
                if (result.Count == 0) {
                    webReqFails.ArtistList++;
                    if (webReqFails.ArtistList == webReqFails.MaxFails) {
                        logger?.Log("Reached max fails for GetArtistList.");
                    }
                } else {
                    webReqFails.ArtistList = 0;
                }
                song.ArtistList = result;
                if (song.ArtistList.Count == 0) {
                    song.ArtistList = null;
                }

            }

            // ================================================
            // Get music url
            // ------------------------------------------------

            if (song.SongUrl == null && webReqFails.SongUrl < webReqFails.MaxFails) {
                var result = await webScraper.GetSongUrl();
                if (!IsCurrentSong(song)) return;
                if (result == null) {
                    webReqFails.SongUrl++;
                    if (webReqFails.SongUrl == webReqFails.MaxFails) {
                        logger?.Log("Reached max fails for GetSongUrl.");
                    }
                } else {
                    webReqFails.SongUrl = 0;
                }
                song.SongUrl = result;
            }

            // ================================================
            // Get artist url
            // ------------------------------------------------

            if (song.ArtistUrl == null && webReqFails.ArtistUrl < webReqFails.MaxFails) {
                var result = await webScraper.GetArtistUrl();
                if (!IsCurrentSong(song)) return;
                if (result == null) {
                    webReqFails.ArtistUrl++;
                    if (webReqFails.ArtistUrl == webReqFails.MaxFails) {
                        logger?.Log("Reached max fails for GetArtistUrl.");
                    }
                } else {
                    webReqFails.ArtistUrl = 0;
                }
                song.ArtistUrl = result;
            }

            // ================================================
            // Get lyrics
            // ------------------------------------------------

            if (!song.LyricsSearched && AMWin_RichPresence.Properties.Settings.Default.EnableSyncLyrics) {
                song.LyricsSearched = true;
                var result = await lrclibClient.GetSyncedLyrics(song.SongName, song.SongArtist, song.SongDuration);
                if (!IsCurrentSong(song)) return;
                if (result != null) {
                    song.SyncedLyrics = result.Lyrics;
                }
            }
        }

        // e.g. parse "-1:30" to 90 seconds
        private static int? ParseTimeString(string? time) {

            if (string.IsNullOrWhiteSpace(time)) {
                return null;
            }

            // A valid time string should not be the song name (case-insensitive check for common time-like titles)
            // Most durations are less than an hour, and Apple Music format is usually M:SS or -M:SS
            if (!Regex.IsMatch(time, @"^-?\d{1,3}:\d{2}$")) {
                return null;
            }

            // remove leading "-"
            string cleanTime = time;
            if (cleanTime.Contains('-')) {
                cleanTime = cleanTime.Split('-')[1];
            }

            var parts = cleanTime.Split(':');
            if (parts.Length < 2) return null;

            if (int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int sec)) {
                return min * 60 + sec;
            }

            return null;
        }

        private static Tuple<string, string, string?> ParseSongAlbumArtist(string songAlbumArtist, bool composerAsArtist) {
            string songArtist;
            string songAlbum;
            string? songPerformer = null;

            // some classical songs add "By " before the composer's name
            var songComposerPerformer = ComposerPerformerRegex.Matches(songAlbumArtist);
            if (songComposerPerformer.Count > 0) {
                var songComposer = songAlbumArtist.Split(" \u2014 ")[0].Remove(0, 3);
                songPerformer = songAlbumArtist.Split(" \u2014 ")[1];
                songArtist = composerAsArtist ? songComposer : songPerformer;
                songAlbum = songAlbumArtist.Split(" \u2014 ")[2];
            } else {
                // U+2014 is the emdash used by the Apple Music app, not the standard "-" character on the keyboard!
                var songSplit = songAlbumArtist.Split(" \u2014 ");
                if (songSplit.Length > 1) {
                    songArtist = songSplit[0];
                    songAlbum = songSplit[1];
                } else { // no emdash, probably custom music
                    // TODO find a better way to handle this?
                    songArtist = songSplit[0];
                    songAlbum = songSplit[0];
                }
            }
            return new(songArtist, songAlbum, songPerformer);
        }

    }
}
