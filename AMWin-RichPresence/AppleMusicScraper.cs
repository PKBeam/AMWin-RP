using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows.Automation;

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
        public string   CoverArtUrl;

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

    internal class AppleMusicScraper {

        public delegate void RefreshHandler(AppleMusicInfo? newInfo);

        Timer timer;

        // if null, Apple Music is not open
        AppleMusicInfo? amInfo;

        AutomationElement? appleMusicWindow;
        
        RefreshHandler refreshHandler;

        public AppleMusicScraper(int refreshPeriodInSec, RefreshHandler refreshHandler) {
            this.refreshHandler = refreshHandler;

            timer = new Timer(refreshPeriodInSec * 1000);
            timer.Elapsed += Refresh;
            Refresh(this, null);
            timer.Start();
        }

        public void Refresh(Object? source, ElapsedEventArgs e) {
            if (appleMusicWindow == null) {
                FindAppleMusicWindow();
            } 
            amInfo = GetAppleMusicInfo();
            refreshHandler(amInfo);
        }
    

        public void FindAppleMusicWindow() {
            var allWindows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);
            foreach (AutomationElement element in allWindows) {
                var elementProperties = element.Current;
                // TODO - How do we tell it's the actual Windows-native Apple Music application and not some other one?
                if (elementProperties.Name == "Apple Music" && elementProperties.ClassName == "WinUIDesktopWin32WindowClass") {
                    appleMusicWindow = element;
                }
            }
        }

        public AppleMusicInfo? GetAppleMusicInfo() {
            if (appleMusicWindow == null) {
                return null;
            }
            var amWindow = appleMusicWindow;
            var amWinChild = amWindow.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "DesktopChildSiteBridge"));
            var songFields = amWinChild.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "myScrollViewer"));

            if (songFields.Count != 2) {
                return AppleMusicInfo.NoSong();
            }

            // Get song info

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

            var currentTimeElement = amWinChild.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "CurrentTime"));
            var remainingDurationElement = amWinChild.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "Duration"));
            
            var currentTime = ParseTimeString(currentTimeElement.Current.Name);
            var remainingDuration = ParseTimeString(remainingDurationElement.Current.Name);

            // check if the song is paused or not
            var playPauseButton = amWinChild.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "TransportControl_PlayPauseStop"));
            var songIsPaused = playPauseButton.Current.Name == "Play";

            var amInfo = new AppleMusicInfo() {
                HasSong = true,
                IsPaused = songIsPaused,
                SongName = songName,
                SongSubTitle = songAlbumArtist,
                SongAlbum = songAlbum,
                SongArtist = songArtist,
                PlaybackStart = DateTime.UtcNow - new TimeSpan(0, 0, currentTime),
                PlaybackEnd = DateTime.UtcNow + new TimeSpan(0, 0, remainingDuration),
                CoverArtUrl = GetAlbumArtUrl(songName, songAlbum, songArtist)
            };

            return amInfo;
        }

        // e.g. parse "-1:30" to 90 seconds
        private int ParseTimeString(string time) {

            // remove leading "-"
            if (time.Contains('-')) {
                time = time.Split('-')[1];
            }

            int min = int.Parse(time.Split(":")[0]);
            int sec = int.Parse(time.Split(":")[1]);

            return min * 60 + sec;
        }

        private static string GetAlbumArtUrl(string songName, string songAlbum, string songArtist) {
            var url = $"https://music.apple.com/us/search?term={songName} {songAlbum} {songArtist}";
            var web = new HtmlWeb();
            var doc = web.Load(url);
            var list = doc.DocumentNode
                .Descendants("ul")
                .Where(x => x.Attributes["class"].Value.Contains("grid--top-results"))
                .ToList();

            var imgSources = list[0].ChildNodes[0]
                .Descendants("source")
                .Where(x => x.Attributes["type"].Value == "image/jpeg")
                .ToList();

            var x = imgSources[0].Attributes["srcset"].Value;
            return x.Split(' ')[0];
        }
    }
}
