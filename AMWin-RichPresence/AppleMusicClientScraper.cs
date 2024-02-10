using System;
using System.Diagnostics;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FlaUI.UIA3;
using FlaUI.Core.Conditions;
using FlaUI.Core.AutomationElements;

namespace AMWin_RichPresence {

    internal class AppleMusicInfo {
        // the following fields are only valid if HasSong is true.
        // DateTimes are in UTC.
        public string SongName;
        public string SongSubTitle;
        public string SongAlbum;
        public string SongArtist;
        public bool IsPaused = true;
        public DateTime? PlaybackStart;
        public DateTime? PlaybackEnd;
        public int? SongDuration = null;
        public List<string>? ArtistList = null;
        public string? CoverArtUrl = null;
        public int? CurrentTime = null;

        public AppleMusicInfo(string songName, string songSubTitle, string songAlbum, string songArtist) {
            this.SongName = songName;
            this.SongSubTitle = songSubTitle;
            this.SongAlbum = songAlbum;
            this.SongArtist = songArtist;
        }

        public override string ToString() {
            var str = $"[AppleMusicInfo] {SongName} by {SongArtist} on {SongAlbum}";
            if (SongDuration != null) {
                str += $"\n| Duration:  {SongDuration} sec";
            }
            if (CoverArtUrl != null) {
                str += $"\n| Album art: {CoverArtUrl}";
            }
            return str;
        }
        public void Print() {
            Trace.WriteLine(ToString());
        }
    }

    internal class AppleMusicClientScraper {
        private static readonly Regex ComposerPerformerRegex = new Regex(@"By\s.*?\s\u2014", RegexOptions.Compiled);

        public delegate void RefreshHandler(AppleMusicInfo? newInfo);
        string? lastFmApiKey;
        Timer timer;
        RefreshHandler refreshHandler;
        AppleMusicInfo? currentSong;
        public bool composerAsArtist; // for classical music, treat composer (not performer) as artist
        Logger? logger;

        public AppleMusicClientScraper(string? lastFmApiKey, int refreshPeriodInSec, bool composerAsArtist, RefreshHandler refreshHandler, Logger? logger = null) {
            this.refreshHandler = refreshHandler;
            this.logger = logger;
            this.lastFmApiKey = lastFmApiKey;
            timer = new Timer(refreshPeriodInSec * 1000);
            timer.Elapsed += Refresh;
            Refresh(this, null);
            timer.Start();
            this.composerAsArtist = composerAsArtist;
        }

        ~AppleMusicClientScraper() {
            timer.Elapsed -= Refresh;
        }

        public void Refresh(object? source, ElapsedEventArgs? e) {
            AppleMusicInfo? appleMusicInfo = null;
            try {
                appleMusicInfo = GetAppleMusicInfo();
            } catch (Exception ex) {
                logger?.Log($"Something went wrong while scraping: {ex}");
            }
            refreshHandler(appleMusicInfo);
        }

        public AppleMusicInfo? GetAppleMusicInfo() {
            var amProcesses = Process.GetProcessesByName("AppleMusic");
            if (amProcesses.Length == 0) {
                logger?.Log("Could not find an AppleMusic.exe process");
                return null;
            }
            var app = FlaUI.Core.Application.Attach(amProcesses[0].Id);
            using (var automation = new UIA3Automation()) {
                var window = app.GetMainWindow(automation);
                var amWinTransportBar = FindFirstDescendantWithAutomationId(window, "TransportBar");
                if (amWinTransportBar == null) {
                    logger?.Log("Apple Music song panel (TransportBar) is not initialised or missing");
                    return null;
                }
                var amWinLCD = amWinTransportBar.FindFirstChild("LCD");

                // song panel not initialised
                if (amWinLCD == null) {
                    logger?.Log("Apple Music song panel (LCD) is not initialised or missing");
                    return null;
                }

                var songFields = amWinLCD.FindAllChildren(new ConditionFactory(new UIA3PropertyLibrary()).ByAutomationId("myScrollViewer"));

                // ================================================
                //  Check if there is a song playing
                // ------------------------------------------------

                if (songFields.Length != 2) {
                    return null;
                }

                // ================================================
                //  Get song info
                // ------------------------------------------------

                var songNameElement = songFields[0];
                var songAlbumArtistElement = songFields[1];


                // the upper rectangle is the song name; the bottom rectangle is the author/album
                // lower .Bottom = higher up on the screen (?)
                if (songNameElement.BoundingRectangle.Bottom > songAlbumArtistElement.BoundingRectangle.Bottom) {
                    songNameElement = songFields[1];
                    songAlbumArtistElement = songFields[0];
                }

                var songName = songNameElement.Name;
                var songAlbumArtist = songAlbumArtistElement.Name;

                string songArtist = "";
                string songAlbum = "";

                // some classical songs add "By " before the composer's name
                string? songComposer = null;
                string? songPerformer = null;
                //var songComposerPerformer = ComposerPerformerRegex.Matches(songAlbumArtist);
                try {
                    var songInfo = ParseSongAlbumArtist(songAlbumArtist, composerAsArtist);
                    songArtist = songInfo.Item1;
                    songAlbum = songInfo.Item2;
                } catch (Exception ex) {
                    logger?.Log($"Could not parse '{songAlbumArtist}' into artist and album: {ex}");
                }

                // when searching for song info, use the performer as the artist instead of composer
                string songSearchArtist = songPerformer ?? songArtist;

                // if this is a new song, clear out the current song
                if (currentSong == null || currentSong?.SongName != songName || currentSong?.SongArtist != songArtist || currentSong?.SongSubTitle != songAlbumArtist) {
                    currentSong = new AppleMusicInfo(songName, songAlbumArtist, songAlbum, songArtist);
                }

                // init web scraper
                var webScraper = new AppleMusicWebScraper(songName, songAlbum, songSearchArtist, logger, lastFmApiKey);

                // find artist list... unless it's a classical song
                if (currentSong.ArtistList == null && songComposer == null) {
                    currentSong.ArtistList = webScraper.GetArtistList();
                    if (currentSong.ArtistList.Count == 0) {
                        currentSong.ArtistList = null;
                    }
                }
                // ================================================
                //  Get song timestamps
                // ------------------------------------------------

                var currentTimeElement = amWinLCD.FindFirstChild("CurrentTime");
                var remainingDurationElement = amWinLCD.FindFirstChild("Duration");

                // grab the seek slider to check song playback progress
                var songProgressSlider = amWinLCD
                    .FindFirstChild("LCDScrubber")? // this may be hidden when a song is initialising
                    .Patterns.RangeValue.Pattern;

                var songProgressPercent = songProgressSlider == null ? 0 : songProgressSlider.Value / songProgressSlider.Maximum;

                // calculate song timestamps
                int? currentTime = null;
                int? remainingDuration = null;

                // if the timestamps are being hidden by Apple Music, we fall back to independent timestamp calculation
                if (currentTimeElement == null || remainingDurationElement == null) {

                    // try to get song duration if we don't have it
                    if (currentSong.SongDuration == null) {
                        webScraper.GetSongDuration().ContinueWith(t => {
                            string? dur = t.Result;
                            currentSong.SongDuration = dur == null ? null : ParseTimeString(dur);
                        });
                    }

                    // if success, set timestamps
                    if (currentSong.SongDuration != null) {
                        var songDuration = currentSong.SongDuration;
                        currentTime = (int)(songProgressPercent * songDuration);
                        remainingDuration = (int)((1 - songProgressPercent) * songDuration);
                    }

                } else { // ... otherwise just use the timestamps provided by Apple Music
                    currentTime = ParseTimeString(currentTimeElement!.Name);
                    remainingDuration = ParseTimeString(remainingDurationElement!.Name);
                }

                currentSong.CurrentTime = currentTime;

                // if we have timestamps, convert them into Unix timestamps for Discord
                if (currentTime != null && remainingDuration != null) {
                    currentSong.PlaybackStart = DateTime.UtcNow - new TimeSpan(0, 0, (int)currentTime);
                    currentSong.PlaybackEnd = DateTime.UtcNow + new TimeSpan(0, 0, (int)remainingDuration);
                }

                // check if the song is paused or not
                var playPauseButton = amWinTransportBar.FindFirstChild("TransportControl_PlayPauseStop");

                currentSong.IsPaused = playPauseButton.Name == "Play";


                // ================================================
                //  Get song cover art
                // ------------------------------------------------

                if (currentSong.CoverArtUrl == null) {
                    webScraper.GetAlbumArtUrl().ContinueWith(t => {
                        currentSong.CoverArtUrl = t.Result;
                    });
                }

            }
            return currentSong;
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

        private static Tuple<string, string> ParseSongAlbumArtist(string songAlbumArtist, bool composerAsArtist) {
            string songArtist;
            string songAlbum;

            // some classical songs add "By " before the composer's name
            string? songComposer = null;
            string? songPerformer = null;
            var songComposerPerformer = ComposerPerformerRegex.Matches(songAlbumArtist);
            if (songComposerPerformer.Count > 0) {
                songComposer = songAlbumArtist.Split(" \u2014 ")[0].Remove(0, 3);
                songPerformer = songAlbumArtist.Split(" \u2014 ")[1];
                songArtist = composerAsArtist ? songComposer : songPerformer;
                songAlbum = songAlbumArtist.Split(" \u2014 ")[2];
            } else {
                // U+2014 is the emdash used by the Apple Music app, not the standard "-" character on the keyboard!
                songArtist = songAlbumArtist.Split(" \u2014 ")[0];
                songAlbum = songAlbumArtist.Split(" \u2014 ")[1];
            }
            return new(songArtist, songAlbum);
        }

        // some localisations of Apple Music have slight differences in element names
        private static string StringToLetters(string s) {
            return new string(s.Where(char.IsLetter).ToArray());
        }
        private static bool StringLetterComparison(string s1, string s2) {
            return StringToLetters(s1) == StringToLetters(s2);
        }

        // breadth-first search for element with given automation ID.
        // BFS is preferred as the elements we want to find are generally not too deep in the element tree
        private static AutomationElement? FindFirstDescendantWithAutomationId(AutomationElement baseElement, string id) {
            List<AutomationElement> nodes = new() { baseElement };
            for (var i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                if (node.Properties.AutomationId.IsSupported && node.AutomationId == id) {
                    return node;
                }
                nodes.AddRange(node.FindAllChildren());
            }
            return null;
        }
    }
}
