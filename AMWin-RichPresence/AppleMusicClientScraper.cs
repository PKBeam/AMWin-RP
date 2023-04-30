using System;
using System.Diagnostics;
using System.Timers;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

namespace AMWin_RichPresence {

    internal struct AppleMusicInfo {
        public bool HasSong;

        // the following fields are only valid if HasSong is true.
        // DateTimes are in UTC.
        public bool     IsPaused;
        public string   SongName;
        public string   SongSubTitle;
        public string   SongAlbum;
        public string   SongArtist;
        public DateTime PlaybackStart;
        public DateTime PlaybackEnd;
        public string?  CoverArtUrl;

        public static AppleMusicInfo NoSong() {
            var amInfo = new AppleMusicInfo();
            amInfo.HasSong = false;
            return amInfo;
        }

        public override string ToString() {
            return $"""
                AppleMusicInfo: 
                - HasSong: {HasSong},
                - IsPaused: {IsPaused},
                - SongName: {SongName},
                - SongAlbum: {SongAlbum},
                - SongArtist: {SongArtist},
                - PlaybackStart: {PlaybackStart},
                - PlaybackEnd: {PlaybackEnd},
                - CoverArtUrl: {CoverArtUrl}
                """;
        }
        public void Print() {
            Trace.WriteLine(ToString());
        }
    }

    internal class AppleMusicClientScraper {

        public delegate void RefreshHandler(AppleMusicInfo? newInfo);
     
        Timer timer;
        RefreshHandler refreshHandler;

        public AppleMusicClientScraper(int refreshPeriodInSec, RefreshHandler refreshHandler) {
            this.refreshHandler = refreshHandler;
            timer = new Timer(refreshPeriodInSec * 1000);
            timer.Elapsed += Refresh;
            Refresh(this, null);
            timer.Start();
        }

        public void Refresh(object? source, ElapsedEventArgs? e) {
            AppleMusicInfo? appleMusicInfo = null;
            AutomationElement? appleMusicWindow;

            appleMusicWindow = FindAppleMusicWindow();
            if (appleMusicWindow != null) {
                appleMusicInfo = GetAppleMusicInfo(appleMusicWindow);
            }
            refreshHandler(appleMusicInfo);
        }

        public static AutomationElement? FindAppleMusicWindow() {
            try {
                var allWindows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);
                foreach (AutomationElement element in allWindows) {
                    var elementProperties = element.Current;
                    // TODO - How do we tell it's the actual Windows-native Apple Music application and not some other one?
                    if (elementProperties.Name == "Apple Music" && elementProperties.ClassName == "WinUIDesktopWin32WindowClass") {
                        return element;
                    }
                }
            } catch (ElementNotAvailableException) {
                return null;
            }
            return null;
        }

        public AppleMusicInfo? GetAppleMusicInfo(AutomationElement amWindow) {
            
            var amInfo = new AppleMusicInfo();

            // ================================================
            //  Check if there is a song playing
            // ------------------------------------------------

            var amWinChild = amWindow.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "DesktopChildSiteBridge"));
            var songFields = amWinChild.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "myScrollViewer"));

            if (songFields.Count != 2) {
                return AppleMusicInfo.NoSong();
            } else {
                amInfo.HasSong = true;
            }

            // ================================================
            //  Get song info
            // ------------------------------------------------
            
            var songNameElement = songFields[0];
            var songAlbumArtistElement = songFields[1];

            // the upper rectangle is the song name; the bottom rectangle is the author/album
            // lower .Bottom = higher up on the screen (?)
            if (songNameElement.Current.BoundingRectangle.Bottom > songAlbumArtistElement.Current.BoundingRectangle.Bottom) {
                songNameElement = songFields[1];
                songAlbumArtistElement = songFields[0];
            }

            var songName = songNameElement.Current.Name;
            var songAlbumArtist = songAlbumArtistElement.Current.Name;
            
            // this is the U+2014 emdash, not the standard "-" character on the keyboard!
            var songArtist = songAlbumArtist.Split(" — ")[0]; 
            var songAlbum = songAlbumArtist.Split(" — ")[1];
            
            amInfo.SongName = songName;
            amInfo.SongSubTitle = songAlbumArtist;
            amInfo.SongArtist = songArtist;
            amInfo.SongAlbum = songAlbum;

            // ================================================
            //  Get song timestamps
            // ------------------------------------------------

            var currentTimeElement = amWinChild.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "CurrentTime"));
            var remainingDurationElement = amWinChild.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "Duration"));

            // grab the seek slider to check song playback progress
            var songProgressElement = amWinChild.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "LCDScrubber"));
            var songProgressSlider = songProgressElement.GetCurrentPattern(RangeValuePattern.Pattern) as RangeValuePattern;
            var songProgressPercent = songProgressSlider.Current.Value / songProgressSlider.Current.Maximum;

            // calculate song timestamps
            int currentTime;
            int remainingDuration;

            // if the timestamps are being hidden by Apple Music, we fall back to independent timestamp calculation
            if (currentTimeElement == null || remainingDurationElement == null) {
                var songDuration = ParseTimeString(AppleMusicWebScraper.GetSongDuration(songName, songAlbum, songArtist));
                currentTime = (int)(songProgressPercent * songDuration);
                remainingDuration = (int)((1 - songProgressPercent) * songDuration);
            } else { // ... otherwise just use the timestamps provided by Apple Music
                currentTime = ParseTimeString(currentTimeElement!.Current.Name);
                remainingDuration = ParseTimeString(remainingDurationElement!.Current.Name);
            }

            // convert into Unix timestamps for Discord
            amInfo.PlaybackStart = DateTime.UtcNow - new TimeSpan(0, 0, currentTime);
            amInfo.PlaybackEnd = DateTime.UtcNow + new TimeSpan(0, 0, remainingDuration);

            // check if the song is paused or not
            var playPauseButton = amWinChild.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "TransportControl_PlayPauseStop"));
            amInfo.IsPaused = playPauseButton.Current.Name == "Play";


            // ================================================
            //  Get song cover art
            // ------------------------------------------------

            amInfo.CoverArtUrl = AppleMusicWebScraper.GetAlbumArtUrl(songName, songAlbum, songArtist);

            return amInfo;
        }

        // e.g. parse "-1:30" to 90 seconds
        private static int ParseTimeString(string time) {

            // remove leading "-"
            if (time.Contains('-')) {
                time = time.Split('-')[1];
            }

            int min = int.Parse(time.Split(":")[0]);
            int sec = int.Parse(time.Split(":")[1]);

            return min * 60 + sec;
        }
    }
}
