using System;
using System.Diagnostics;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FlaUI.UIA3;
using FlaUI.Core.Conditions;
using FlaUI.Core.AutomationElements;
using System.Security.Policy;

namespace AMWin_RichPresence {

    internal class AppleMusicInfo: IEquatable<AppleMusicInfo> {
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
        public string? SongUrl = null;
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

        public bool Equals(AppleMusicInfo? other) {
            return other is not null && other!.SongName == SongName && other!.SongArtist == SongArtist && other!.SongSubTitle == SongSubTitle;
        }
        public override bool Equals(object? obj) => Equals(obj as AppleMusicInfo);
        public static bool operator == (AppleMusicInfo? a1, AppleMusicInfo? a2) {
            if (a1 is null && a2 is null) {
                return true;
            } else if (a1 is null || a2 is null) {
                return false;
            } else {
                return a1.Equals(a2);
            }
        }
        public static bool operator != (AppleMusicInfo? a1, AppleMusicInfo? a2) {
            return !(a1 == a2);
        }

        public override int GetHashCode() {
            return SongName.GetHashCode() ^ SongArtist.GetHashCode() ^ SongSubTitle.GetHashCode();
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
        int failedWebRequests = 0;
        double? previousSongProgress;
        string appleMusicRegion;

        public AppleMusicClientScraper(string? lastFmApiKey, int refreshPeriodInSec, bool composerAsArtist, string appleMusicRegion, RefreshHandler refreshHandler, Logger? logger = null) {
            this.refreshHandler = refreshHandler;
            this.logger = logger;
            this.lastFmApiKey = lastFmApiKey;
            this.composerAsArtist = composerAsArtist;
            this.appleMusicRegion = appleMusicRegion;

            timer = new Timer(refreshPeriodInSec * 1000);
            timer.Elapsed += Refresh;
            Refresh(this, null);
            timer.Start();
        }

        ~AppleMusicClientScraper() {
            timer.Elapsed -= Refresh;
        }

        public void ChangeRegion(string region) {
            this.appleMusicRegion = region;
            Refresh(this, null);
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
            var isMiniPlayer = true;
            var webSearchFailed = false;
            var shouldTrySearch = failedWebRequests < Constants.NumFailedSearchesBeforeAbandon;

            var amProcesses = Process.GetProcessesByName("AppleMusic");
            if (amProcesses.Length == 0) {
                logger?.Log("Could not find an AppleMusic.exe process");
                return null;
            }
            using (var automation = new UIA3Automation()) {
                var windows = automation.GetDesktop().FindAllChildren(c => c.ByProcessId(amProcesses[0].Id));

                // find an apple music window that we can extract information from
                AutomationElement? amSongPanel = null;
                foreach (var window in windows) {
                    // TODO: can localisation change the window name of the Mini Player?
                    isMiniPlayer = window.Name == "Mini Player";

                    if (isMiniPlayer) {
                        amSongPanel = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"));

                        // preference the mini player because it always has timestamps visible
                        if (amSongPanel != null) {
                            break;
                        }
                    } else {
                        amSongPanel = FindFirstDescendantWithAutomationId(window, "TransportBar") ?? amSongPanel;
                    }
                }

                if (amSongPanel == null) {
                    logger?.Log("Apple Music song panel is not initialised or missing");
                    return null;
                }

                // ================================================
                //  Get song fields
                // ------------------------------------------------

                var songFieldsPanel = isMiniPlayer ? amSongPanel : amSongPanel.FindFirstChild("LCD");
                var songFields = songFieldsPanel?.FindAllChildren(new ConditionFactory(new UIA3PropertyLibrary()).ByAutomationId("myScrollViewer")) ?? [];

                // ================================================
                //  Check if there is a song playing
                // ------------------------------------------------

                // an active mini player must have a song 
                if (!isMiniPlayer && songFields.Length != 2) {
                    return null;
                }

                // ================================================
                //  Get song, artist and album names
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
                string? songPerformer = null;

                // parse song string into album and artist
                try {
                    var songInfo = ParseSongAlbumArtist(songAlbumArtist, composerAsArtist);
                    songArtist = songInfo.Item1;
                    songAlbum = songInfo.Item2;
                    songPerformer = songInfo.Item3;
                } catch (Exception ex) {
                    logger?.Log($"Could not parse '{songAlbumArtist}' into artist and album: {ex}");
                }

                // ================================================
                //  Initialise basic song data and web scraper
                // ------------------------------------------------

                var newSong = new AppleMusicInfo(songName, songAlbumArtist, songAlbum, songArtist);

                // only clear out the current song if song is new
                if (currentSong != newSong) {
                    // keep the same album art if it's another song in the same album
                    if (newSong.SongAlbum == currentSong?.SongAlbum && newSong.SongArtist == currentSong?.SongArtist) {
                        newSong.CoverArtUrl = currentSong.CoverArtUrl;
                    }
                    currentSong = newSong;
                    failedWebRequests = 0;
                    previousSongProgress = null;
                }

                // when searching for song info, use the performer as the artist instead of composer
                var webScraper = new AppleMusicWebScraper(songName, songAlbum, songPerformer ?? songArtist, appleMusicRegion, logger, lastFmApiKey);

                // ================================================
                //  Get song playback status
                // ------------------------------------------------

                // check if the song is paused or not
                var playPauseButton = amSongPanel.FindFirstChild("TransportControl_PlayPauseStop");

                // grab playback status directly from Apple Music for English languages
                if (playPauseButton.Name == "Play" || playPauseButton.Name == "Pause") {
                    currentSong.IsPaused = playPauseButton.Name == "Play";

                } else { // ... otherwise fallback to tracking song progress
                    var songProgressSlider = (isMiniPlayer ? amSongPanel.FindFirstChild("Scrubber") : amSongPanel.FindFirstChild("LCD").FindFirstChild("LCDScrubber"))?.Patterns.RangeValue.Pattern;
                    var songProgress = songProgressSlider == null ? 0 : songProgressSlider.Value / songProgressSlider.Maximum;

                    currentSong.IsPaused = previousSongProgress != null && songProgress == previousSongProgress;

                    previousSongProgress = songProgress;
                }

                // ================================================
                //  Get song timestamps
                // ------------------------------------------------

                int? currentTime = null;
                int? remainingDuration = null;
                
                var currentTimeElement = amSongPanel.FindFirstChild("CurrentTime");
                var remainingDurationElement = amSongPanel.FindFirstChild("Duration");

                // use the Apple Music timestamps, if visible
                if (currentTimeElement != null && remainingDurationElement != null) {
                    currentTime = ParseTimeString(currentTimeElement!.Name);
                    remainingDuration = ParseTimeString(remainingDurationElement!.Name);

                } else { // fallback to calculation using song slider

                    // web query for song duration if we don't have it
                    if (shouldTrySearch && currentSong.SongDuration == null) {
                        webScraper.GetSongDuration().ContinueWith(t => {
                            if (t.Result == null) {
                                webSearchFailed = true;
                            }
                            string? dur = t.Result;
                            currentSong.SongDuration = ParseTimeString(dur);
                        });
                    }

                    // if success, set timestamps using seek slider
                    if (currentSong.SongDuration != null) {

                        // grab the seek slider to check song playback progress
                        var songProgressSlider = amSongPanel.FindFirstChild("LCD").FindFirstChild("LCDScrubber")?.Patterns.RangeValue.Pattern;
                        var songProgressPercent = songProgressSlider == null ? 0 : songProgressSlider.Value / songProgressSlider.Maximum;

                        currentTime = (int)(songProgressPercent * currentSong.SongDuration);
                        remainingDuration = (int)((1 - songProgressPercent) * currentSong.SongDuration);
                    }

                }

                currentSong.CurrentTime = currentTime;

                // convert timestamps to Unix format for Discord
                if (currentTime != null && remainingDuration != null) {
                    currentSong.PlaybackStart = DateTime.UtcNow - new TimeSpan(0, 0, (int)currentTime);
                    currentSong.PlaybackEnd = DateTime.UtcNow + new TimeSpan(0, 0, (int)remainingDuration);
                }

                // ================================================
                //  Get song cover art  
                // ------------------------------------------------

                if (shouldTrySearch && currentSong.CoverArtUrl == null) {
                    webScraper.GetAlbumArtUrl().ContinueWith(t => {
                        if (t.Result == null) {
                            webSearchFailed = true;
                        }
                        currentSong.CoverArtUrl = t.Result;
                    });
                }

                // ================================================
                //  Get song artists, as a list
                // ------------------------------------------------

                if (shouldTrySearch && currentSong.ArtistList == null) {
                    webScraper.GetArtistList().ContinueWith(t => {
                        if (t.Result.Count == 0) {
                            webSearchFailed = true;
                        }
                        currentSong.ArtistList = t.Result;
                        if (currentSong.ArtistList.Count == 0) {
                            currentSong.ArtistList = null;
                        }
                    });
                }

                // ================================================
                // Get music url
                // ------------------------------------------------

                if (shouldTrySearch && currentSong.SongUrl == null) {
                    webScraper.GetSongUrl().ContinueWith(t => {
                        if (t.Result == null) {
                            webSearchFailed = true;
                        }
                        currentSong.SongUrl = t.Result;
                    });
                }
            }

            if (webSearchFailed) {
                failedWebRequests += 1;
            }

            return currentSong;
        }

        // e.g. parse "-1:30" to 90 seconds
        private static int? ParseTimeString(string? time) {

            if (time == null) {
                return null;
            }

            // remove leading "-"
            if (time.Contains('-')) {
                time = time.Split('-')[1];
            }

            int min = int.Parse(time.Split(":")[0]);
            int sec = int.Parse(time.Split(":")[1]);

            return min * 60 + sec;
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
                // fallback to prevent this taking too long
                if (nodes.Count > 25) {
                    return null;
                }
            }
            return null;
        }
    }
}
