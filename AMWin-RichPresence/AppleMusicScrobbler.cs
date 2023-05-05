using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using IF.Lastfm.Core.Scrobblers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
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
        private HttpClient? httpClient;
        private int elapsedSeconds;
        private string? lastSongID;
        private bool hasScrobbled;

        private string CleanAlbumName(string songName) {
            // Remove " - Single" and " - EP"
            var re = new Regex(@"\s-\s((Single)|(EP))$");
            return re.Replace(songName, new MatchEvaluator((m) => { return ""; }));
        }

        public async void init(LastFmCredentials credentials, bool showMessageBoxOnSuccess = false)
        {
            if (!String.IsNullOrEmpty(credentials.apiKey) 
                && !String.IsNullOrEmpty(credentials.apiSecret)
                && !String.IsNullOrEmpty(credentials.username))
            {
                // Use the four pieces of information (API Key, API Secret, Username, Password) to log into Last.FM for Scrobbling
                httpClient = new HttpClient();
                lastfmAuth = new LastAuth(credentials.apiKey, credentials.apiSecret);
                await lastfmAuth.GetSessionTokenAsync(credentials.username, credentials.password);

                if (lastfmAuth.Authenticated) {
                    if (showMessageBoxOnSuccess) {
                        MessageBox.Show("The Last.FM credentials were successfully authenticated.", "Last.FM Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                } else {
                    MessageBox.Show("The Last.FM credentials could not be authenticated. Please make sure you have entered the correct username and password, and that your account is not currently locked.", "Last.FM Authentication", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                lastFmScrobbler = new MemoryScrobbler(lastfmAuth, httpClient);
            }
        }

        public IScrobbler? GetLastFmScrobbler()
        {
            return lastFmScrobbler;
        }

        public void UpdateCreds(LastFmCredentials credentials, bool showMessageBoxOnSuccess)
        {
            httpClient = null;
            lastfmAuth = null;
            lastFmScrobbler = null;
            init(credentials, showMessageBoxOnSuccess);
        }


        public async void Scrobbleit(AppleMusicInfo info, IScrobbler lastFmScrobbler)
        {
            // This gets called every five seconds (Constants.RefreshPeriod) when a song is playing. There are some rules before we want to scrobble.
            // First, when the song changes, start start "our" timer over at 0.  Every time this gets called, increment by five seconds (RefreshPeriod).
            // If we hit the threshhold (Constants.LastFMTimeBeforeScrobbling) then go ahead and Scrobble it.  Note that this works well because this method
            //    never gets called when the song is paused!  Also, make sure that we don't keep re-Scrobbling, so set a variable "hasScrobbled" for each song.
            //
            // Important caveat:  this does not have any "Scrobbler queue" built in - so only real-time Scrobbling will work (no offline capability).  Fair trade-off
            //    until an official Scrobbler is released.


            try
            {
                string thisSongID = info.SongArtist + info.SongName + info.SongAlbum;

                if (thisSongID != lastSongID)
                {
                    lastSongID = thisSongID;
                    elapsedSeconds = 0;
                    hasScrobbled = false;
                    Trace.WriteLine(string.Format("{0} LastFM Scrobbler - New Song: {1}", DateTime.UtcNow.ToString(), lastSongID));
                }
                else
                {
                    elapsedSeconds += Constants.RefreshPeriod;
                    if (elapsedSeconds > Constants.LastFMTimeBeforeScrobbling && !hasScrobbled)
                    {
                        if (lastfmAuth != null && lastfmAuth.Authenticated)
                        {
                            Trace.WriteLine(string.Format("{0} LastFM Scrobbler - Scrobbling: {1}", DateTime.UtcNow.ToString(), lastSongID));
                            var scrobble = new Scrobble(
                                Properties.Settings.Default.LastfmScrobblePrimaryArtist ? AppleMusicWebScraper.GetArtistList(info.SongName, info.SongAlbum, info.SongArtist).First() : info.SongArtist,
                                Properties.Settings.Default.LastfmCleanAlbumName ? CleanAlbumName(info.SongAlbum) : info.SongAlbum, 
                                info.SongName, 
                                DateTime.UtcNow);
                            var response = await lastFmScrobbler.ScrobbleAsync(scrobble);
                        }
                        hasScrobbled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);

            }
        }


    }
}
