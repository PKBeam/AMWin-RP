using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using IF.Lastfm.Core.Scrobblers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace AMWin_RichPresence
{
    public struct LastFmCredentials {
        public string apiKey;
        public string apiSecret;
        public string username;
        public string password;
    }
    internal class AppleMusicScrobbler
    {

        private LastAuth? lastfmAuth;
        private IScrobbler? lastFmScrobbler;
        private ITrackApi? trackApi;
        private HttpClient? httpClient;
        private int elapsedSeconds;
        private string? lastSongID;
        private bool hasScrobbled;
        private double lastSongProgress;
        private Logger? logger;

        public AppleMusicScrobbler(Logger? logger = null) { 
            this.logger = logger; 
        }           
        
        public static string CleanAlbumName(string songName) {
            // Remove " - Single" and " - EP"
            var re = new Regex(@"\s-\s((Single)|(EP))$");
            return re.Replace(songName, new MatchEvaluator((m) => { return ""; }));
        }

        public async Task<bool> init(LastFmCredentials credentials)
        {
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

        public IScrobbler? GetLastFmScrobbler()
        {
            return lastFmScrobbler;
        }

        public ITrackApi? GetTrackApi()
        {
            return trackApi;
        }

        public async Task<bool> UpdateCredsAsync(LastFmCredentials credentials) {
            logger?.Log("[Last.FM scrobbler] Updating credentials");
            httpClient = null;
            lastfmAuth = null;
            lastFmScrobbler = null;
            trackApi = null;
            return await init(credentials);
        }

        public async void Scrobbleit(AppleMusicInfo info, IScrobbler lastFmScrobbler, ITrackApi trackApi)
        {
            // This gets called every five seconds (Constants.RefreshPeriod) when a song is playing. There are some rules before we want to scrobble.
            // First, when the song changes, start start "our" timer over at 0.  Every time this gets called, increment by five seconds (RefreshPeriod).
            // If we hit the threshold (Constants.LastFMTimeBeforeScrobbling) then go ahead and Scrobble it.  Note that this works well because this method
            //    never gets called when the song is paused!  Also, make sure that we don't keep re-Scrobbling, so set a variable "hasScrobbled" for each song.
            //
            // Important caveat:  this does not have any "Scrobbler queue" built in - so only real-time Scrobbling will work (no offline capability).  Fair trade-off
            //    until an official Scrobbler is released.

            try
            {
                var thisSongID = info.SongArtist + info.SongName + info.SongAlbum;
                var webScraper = new AppleMusicWebScraper(info.SongName, info.SongAlbum, info.SongArtist);
                var scrobble = new Scrobble(
                    Properties.Settings.Default.LastfmScrobblePrimaryArtist ? webScraper.GetArtistList().FirstOrDefault(info.SongArtist) : info.SongArtist,
                    Properties.Settings.Default.LastfmCleanAlbumName ? CleanAlbumName(info.SongAlbum) : info.SongAlbum,
                    info.SongName,
                    DateTime.UtcNow);

                if (thisSongID != lastSongID)
                {
                    lastSongID = thisSongID;
                    elapsedSeconds = 0;
                    hasScrobbled = false;
                    logger?.Log($"[Last.FM scrobbler] New Song: {lastSongID}");

                    await trackApi.UpdateNowPlayingAsync(scrobble);
                    logger?.Log($"[Last.FM scrobbler] Updated now playing: {lastSongID}");
                }
                else
                {
                    elapsedSeconds += Constants.RefreshPeriod;

                    if (hasScrobbled && IsRepeating(info))
                    {
                        hasScrobbled = false;
                        elapsedSeconds = 0;
                        logger?.Log($"[Last.FM scrobbler] Repeating Song: {lastSongID}");
                    }

                    if (IsTimeToScrobble(info) && !hasScrobbled)
                    {
                        if (lastfmAuth != null && lastfmAuth.Authenticated)
                        {
                            logger?.Log($"[Last.FM scrobbler] Scrobbling: {lastSongID}");
                            
                            var response = await lastFmScrobbler.ScrobbleAsync(scrobble);
                        }
                        hasScrobbled = true;
                    }

                    lastSongProgress = info.CurrentTime ?? 0.0;
                }
            }
            catch (Exception ex)
            {
                logger?.Log($"[Last.FM scrobbler] An error occurred while scrobbling: {ex}");
            }
        }

        private bool IsTimeToScrobble(AppleMusicInfo info)
        {
            if (info.SongDuration.HasValue && info.SongDuration.Value >= 30 ) { // we should only scrobble tracks with more than 30 seconds
                double halfSongDuration = info.SongDuration.Value / 2;
                return elapsedSeconds >= halfSongDuration || elapsedSeconds >= 240; // half the song has passed or more than 4 minutes
            }
            return elapsedSeconds > Constants.LastFMTimeBeforeScrobbling;
        }
        private bool IsRepeating(AppleMusicInfo info) {
            if (info.CurrentTime.HasValue && info.SongDuration.HasValue)
            {
                double currentTime = info.CurrentTime.Value;
                double songDuration = info.SongDuration.Value;
                double repeatThreshold =  1.5 * Constants.RefreshPeriod;
                return currentTime <= repeatThreshold && lastSongProgress >= (songDuration - repeatThreshold);
            }

            return false;
        }
    }
}
