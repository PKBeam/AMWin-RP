using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using IF.Lastfm.Core.Scrobblers;
using MetaBrainz.ListenBrainz;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace AMWin_RichPresence {
    internal interface IScrobblerCredentials { }

    public struct LastFmCredentials : IScrobblerCredentials {
        public string apiKey;
        public string apiSecret;
        public string username;
        public string password;
    }

    public struct ListenBrainzCredentials : IScrobblerCredentials {
        public string userToken;
    }

    internal class AlbumCleaner {

        public static string CleanAlbumName(string songName) {
            // Remove " - Single" and " - EP"
            var re = new Regex(@"\s-\s((Single)|(EP))$");
            return re.Replace(songName, new MatchEvaluator((m) => { return ""; }));
        }

    }

    internal abstract class AppleMusicScrobbler<C> where C : IScrobblerCredentials {
        protected int elapsedSeconds;
        protected string? lastSongID;
        protected bool hasScrobbled;
        protected double lastSongProgress;
        protected Logger? logger;
        protected string serviceName;

        public AppleMusicScrobbler(string serviceName, Logger? logger = null) {
            this.serviceName = serviceName;
            this.logger = logger;
        }

        protected bool IsTimeToScrobble(AppleMusicInfo info) {
            if (info.SongDuration.HasValue && info.SongDuration.Value >= 30) { // we should only scrobble tracks with more than 30 seconds
                double halfSongDuration = info.SongDuration.Value / 2;
                return elapsedSeconds >= halfSongDuration || elapsedSeconds >= 240; // half the song has passed or more than 4 minutes
            }
            return elapsedSeconds > Constants.LastFMTimeBeforeScrobbling;
        }

        protected bool IsRepeating(AppleMusicInfo info) {
            if (info.CurrentTime.HasValue && info.SongDuration.HasValue) {
                double currentTime = info.CurrentTime.Value;
                double songDuration = info.SongDuration.Value;
                double repeatThreshold = 1.5 * Constants.RefreshPeriod;
                return currentTime <= repeatThreshold && lastSongProgress >= (songDuration - repeatThreshold);
            }

            return false;
        }

        public abstract Task<bool> init(C credentials);

        public abstract Task<bool> UpdateCredsAsync(C credentials);

        protected abstract Task UpdateNowPlaying(string artist, string album, string song);

        protected abstract Task ScrobbleSong(string artist, string album, string song);

        public async void Scrobbleit(AppleMusicInfo info) {
            // This gets called every five seconds (Constants.RefreshPeriod) when a song is playing. There are some rules before we want to scrobble.
            // First, when the song changes, start start "our" timer over at 0.  Every time this gets called, increment by five seconds (RefreshPeriod).
            // If we hit the threshold (Constants.LastFMTimeBeforeScrobbling) then go ahead and Scrobble it.  Note that this works well because this method
            //    never gets called when the song is paused!  Also, make sure that we don't keep re-Scrobbling, so set a variable "hasScrobbled" for each song.
            //
            // Important caveat:  this does not have any "Scrobbler queue" built in - so only real-time Scrobbling will work (no offline capability).  Fair trade-off
            //    until an official Scrobbler is released.

            try {
                var thisSongID = info.SongArtist + info.SongName + info.SongAlbum;
                var webScraper = new AppleMusicWebScraper(info.SongName, info.SongAlbum, info.SongArtist);
                var artist = Properties.Settings.Default.LastfmScrobblePrimaryArtist ? webScraper.GetArtistList().FirstOrDefault(info.SongArtist) : info.SongArtist;
                var album = Properties.Settings.Default.LastfmCleanAlbumName ? AlbumCleaner.CleanAlbumName(info.SongAlbum) : info.SongAlbum;

                if (thisSongID != lastSongID) {
                    lastSongID = thisSongID;
                    elapsedSeconds = 0;
                    hasScrobbled = false;
                    logger?.Log($"[{serviceName} scrobbler] New Song: {lastSongID}");

                    await UpdateNowPlaying(artist, album, info.SongName);
                    logger?.Log($"[{serviceName} scrobbler] Updated now playing: {lastSongID}");
                } else {
                    elapsedSeconds += Constants.RefreshPeriod;

                    if (hasScrobbled && IsRepeating(info)) {
                        hasScrobbled = false;
                        elapsedSeconds = 0;
                        logger?.Log($"[{serviceName} scrobbler] Repeating Song: {lastSongID}");
                    }

                    if (IsTimeToScrobble(info) && !hasScrobbled) {
                        logger?.Log($"[{serviceName} scrobbler] Scrobbling: {lastSongID}");
                        await ScrobbleSong(artist, album, info.SongName);
                        hasScrobbled = true;
                    }

                    lastSongProgress = info.CurrentTime ?? 0.0;
                }
            } catch (Exception ex) {
                logger?.Log($"[{serviceName} scrobbler] An error occurred while scrobbling: {ex}");
            }
        }
    }

    internal class AppleMusicLastFmScrobbler : AppleMusicScrobbler<LastFmCredentials> {
        private LastAuth? lastfmAuth;
        private IScrobbler? lastFmScrobbler;
        private ITrackApi? trackApi;
        private HttpClient? httpClient;

        public AppleMusicLastFmScrobbler(Logger? logger = null) : base("Last.FM", logger) { }

        public async override Task<bool> init(LastFmCredentials credentials) {
            if (string.IsNullOrEmpty(credentials.apiKey)
                || string.IsNullOrEmpty(credentials.apiSecret)
                || string.IsNullOrEmpty(credentials.username)) {
                return false;
            }
            // Use the four pieces of information (API Key, API Secret, Username, Password) to log into Last.FM for Scrobbling
            httpClient = new HttpClient();
            lastfmAuth = new LastAuth(credentials.apiKey, credentials.apiSecret);
            await lastfmAuth.GetSessionTokenAsync(credentials.username, credentials.password);

            lastFmScrobbler = new MemoryScrobbler(lastfmAuth, httpClient);
            trackApi = new TrackApi(lastfmAuth, httpClient);

            if (lastfmAuth.Authenticated) {
                logger?.Log("Last.FM authentication succeeded");
            } else {
                logger?.Log("Last.FM authentication failed");
            }

            return lastfmAuth.Authenticated;
        }

        public async override Task<bool> UpdateCredsAsync(LastFmCredentials credentials) {
            logger?.Log("[Last.FM scrobbler] Updating credentials");
            httpClient = null;
            lastfmAuth = null;
            lastFmScrobbler = null;
            trackApi = null;
            return await init(credentials);
        }

        protected async override Task ScrobbleSong(string artist, string album, string song) {
            if (lastFmScrobbler == null || lastfmAuth?.Authenticated != true) {
                return;
            }

            var scrobble = new Scrobble(artist, album, song, DateTime.UtcNow);
            await lastFmScrobbler.ScrobbleAsync(scrobble);
        }

        protected async override Task UpdateNowPlaying(string artist, string album, string song) {
            if (trackApi == null || !lastfmAuth?.Authenticated != true) {
                return;
            }

            var scrobble = new Scrobble(artist, album, song, DateTime.UtcNow);
            await trackApi.UpdateNowPlayingAsync(scrobble);
        }
    }

    internal class AppleMusicListenBrainzScrobbler : AppleMusicScrobbler<ListenBrainzCredentials> {
        private ListenBrainz? listenBrainzClient;

        public AppleMusicListenBrainzScrobbler(Logger? logger = null) : base("ListenBrainz", logger) { }

        public async override Task<bool> init(ListenBrainzCredentials credentials) {
            listenBrainzClient = new();

            if (string.IsNullOrEmpty(credentials.userToken)) {
                logger?.Log("No ListenBrainz user token found");
                return false;
            }

            var tokenValidation = await listenBrainzClient.ValidateTokenAsync(credentials.userToken);

            if (tokenValidation.Valid == true) {
                logger?.Log("ListenBrainz authentication succeeded");
                listenBrainzClient.UserToken = credentials.userToken;
            } else {
                logger?.Log("ListenBrainz authentication failed");
                listenBrainzClient.UserToken = null;
            }

            return tokenValidation.Valid ?? false;
        }

        public async override Task<bool> UpdateCredsAsync(ListenBrainzCredentials credentials) {
            logger?.Log("[ListenBrainz] Updating credentials");
            return await init(credentials);
        }

        protected async override Task ScrobbleSong(string artist, string album, string song) {
            if (string.IsNullOrEmpty(listenBrainzClient?.UserToken)) {
                return;
            }

            await listenBrainzClient.SubmitSingleListenAsync(song, artist, album);
        }

        protected async override Task UpdateNowPlaying(string artist, string album, string song) {
            if (string.IsNullOrEmpty(listenBrainzClient?.UserToken)) {
                return;
            }

            await listenBrainzClient.SetNowPlayingAsync(song, artist, album);
        }
    }
}
