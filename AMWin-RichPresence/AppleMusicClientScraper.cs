using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AMWin_RichPresence {
    internal class AppleMusicClientScraper : IPlaybackDataSource {
        private readonly Logger? logger;

        public AppleMusicClientScraper(Logger? logger = null) {
            this.logger = logger;
        }

        public Task<PlaybackData?> GetPlaybackDataAsync() {
            return Task.FromResult(GetPlaybackData());
        }

        public PlaybackData? GetPlaybackData() {
            var amProcesses = Process.GetProcessesByName("AppleMusic");
            var processIds = amProcesses.Select(p => p.Id).ToList();
            foreach (var p in amProcesses) {
                p.Dispose();
            }
            if (processIds.Count == 0) {
                logger?.Log("Could not find an AppleMusic.exe process");
                return null;
            }

            // reads throw COMException 0x80040201 once Apple Music rebuilds its UI tree
            using var automation = new UIA3Automation();

            // find apple music windows
            var windows = new List<AutomationElement>();
            foreach (var processId in processIds) {
                windows.AddRange(Try(() => automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(processId))) ?? []);
            }

            // a display change (docking, undocking, sleep/wake) can drop the windows out of the UIA desktop tree 
            if (windows.Count == 0) {
                logger?.Log("No windows found on desktop, enumerating top-level windows instead");
                foreach (var hWnd in Win32.GetTopLevelWindows(processIds)) {
                    var window = Try(() => automation.FromHandle(hWnd));
                    if (window != null) {
                        windows.Add(window);
                    }
                }
            }

            // find an apple music window that we can extract information from
            AutomationElement? amSongPanel = null;
            var isMiniPlayer = false;
            foreach (var window in windows) {
                // TODO: can localisation change the window name of the Mini Player?
                if (Try(() => window.Name)?.Replace(" ", "") == "MiniPlayer") {
                    // preference the mini player because it always has timestamps visible
                    var miniPanel = Try(() => window.FindFirstDescendant(cf => cf.ByClassName("InputSiteWindowClass")));
                    if (miniPanel != null) {
                        (amSongPanel, isMiniPlayer) = (miniPanel, true);
                        break;
                    }
                } else {
                    amSongPanel ??= Try(() => window.FindFirstDescendant(cf => cf.ByAutomationId("TransportBar")));
                }
            }

            if (isMiniPlayer) {
                logger?.Log("Using Mini Player");
            }

            if (amSongPanel == null) {
                logger?.Log("Apple Music song panel is not initialised or missing");
                return null;
            }

            // ================================================
            //  Get song fields
            // ------------------------------------------------

            var songFieldsPanel = isMiniPlayer ? amSongPanel : amSongPanel.FindFirstChild("LCD");
            var songFields = songFieldsPanel?.FindAllChildren(cf => cf.ByAutomationId("myScrollViewer")) ?? [];

            // ================================================
            //  Check if there is a song playing
            // ------------------------------------------------

            // an active mini player must have a song 
            if (songFields.Length < 2 || (!isMiniPlayer && songFields.Length != 2)) {
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

            // the mini player duplicates the album/artist string by a power of two (when hovered over) to mimic an infinite scroll effect
            if (isMiniPlayer) {
                songName = DeduplicatedString(songName) ?? songName;
                songAlbumArtist = DeduplicatedString(songAlbumArtist) ?? songAlbumArtist;
            }

            var playPauseButton = amSongPanel.FindFirstChild("TransportControl_PlayPauseStop");
            var playPauseName = playPauseButton?.Name;
            bool? isPaused = playPauseName switch {
                "Play" => true,
                "Pause" => false,
                _ => null
            };

            var songProgressSlider = (isMiniPlayer ? amSongPanel.FindFirstChild("Scrubber") : amSongPanel.FindFirstChild("LCD")?.FindFirstChild("LCDScrubber"))?.Patterns.RangeValue.Pattern;
            double? progress = songProgressSlider != null && songProgressSlider.Maximum > 0
                ? songProgressSlider.Value / songProgressSlider.Maximum
                : null;

            return new PlaybackData(
                songName,
                songAlbumArtist,
                isPaused,
                progress,
                songFieldsPanel?.FindFirstChild(cf => cf.ByAutomationId("CurrentTime"))?.Name,
                songFieldsPanel?.FindFirstChild(cf => cf.ByAutomationId("Duration"))?.Name,
                DateTime.UtcNow);
        }

        // one stale window shouldn't abort the whole scrape
        private T? Try<T>(Func<T?> f) where T : class {
            try {
                return f();
            } catch (Exception ex) {
                logger?.Log($"Could not read Apple Music UI: {ex.Message}");
                return null;
            }
        }

        // if the string is duplicated, halve it recursively
        private static string? DeduplicatedString(string s) {
            if (s.Length < 3) return null;

            string firstHalf = s.Substring(0, (s.Length + 1) / 2 - 1);
            string secondHalf = s.Substring((s.Length + 1) / 2);
            if (firstHalf == secondHalf) {
                return DeduplicatedString(firstHalf) ?? firstHalf;
            }
            return null;
        }
    }

    internal static class Win32 {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // includes windows that got cloaked or parked off-screen by a display change
        public static List<IntPtr> GetTopLevelWindows(ICollection<int> processIds) {
            var hWnds = new List<IntPtr>();
            EnumWindows((hWnd, _) => {
                GetWindowThreadProcessId(hWnd, out var processId);
                // cloaked windows still report as visible, so this only drops helper windows
                if (processIds.Contains((int)processId) && IsWindowVisible(hWnd)) {
                    hWnds.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);
            return hWnds;
        }
    }
}
