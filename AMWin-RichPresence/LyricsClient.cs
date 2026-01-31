using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace AMWin_RichPresence {
    public class LrcLine {
        public TimeSpan Time { get; set; }
        public string Text { get; set; } = "";
    }

    internal class LyricsClient {
        private HttpClient httpClient;
        private Logger? logger;
        private const string LRCLIB_API_URL = "https://lrclib.net/api/get";

        public LyricsClient(Logger? logger = null) {
            this.logger = logger;
            this.httpClient = new HttpClient();
            this.httpClient.Timeout = TimeSpan.FromSeconds(15);
            this.httpClient.DefaultRequestHeaders.Add("User-Agent", "AMWin-RichPresence/1.0 (https://github.com/noirg/AMWin-RichPresence)");
        }

        public async Task<List<LrcLine>> GetLyrics(string trackName, string artistName, string albumName, int? duration = null) {
            string cacheDir = System.IO.Path.Combine(Constants.AppDataFolder, "LyricsCache");
            if (!System.IO.Directory.Exists(cacheDir)) {
                System.IO.Directory.CreateDirectory(cacheDir);
            }

            // Sanitize filename
            string safeTrack = string.Join("_", trackName.Split(System.IO.Path.GetInvalidFileNameChars()));
            string safeArtist = string.Join("_", artistName.Split(System.IO.Path.GetInvalidFileNameChars()));
            string cacheFile = System.IO.Path.Combine(cacheDir, $"{safeArtist} - {safeTrack}.json");

            // 1. CHECK CACHE
            if (System.IO.File.Exists(cacheFile)) {
                try {
                    string cachedJson = await System.IO.File.ReadAllTextAsync(cacheFile);
                    logger?.Log($"[LyricsClient] Loaded from cache: {cacheFile}");
                    using (var doc = JsonDocument.Parse(cachedJson)) {
                         var root = doc.RootElement;
                         if (root.TryGetProperty("syncedLyrics", out var syncedLyricsProp)) {
                             return ParseLrc(syncedLyricsProp.GetString() ?? "");
                         }
                    }
                } catch (Exception ex) {
                    logger?.Log($"[LyricsClient] Error reading cache: {ex.Message}");
                }
            }

            // 2. FETCH FROM API
            try {
                // First attempt: Strict search
                var lyricsList = await FetchLyricsInternal(trackName, artistName, albumName, duration, cacheFile);
                
                // Second attempt: Relaxed search (Track + Artist only) if first failed
                if (lyricsList.Count == 0 && (albumName != null || duration != null)) {
                     logger?.Log($"[LyricsClient] Strict search failed for {trackName}, trying relaxed search...");
                     lyricsList = await FetchLyricsInternal(trackName, artistName, null, null, cacheFile);
                }

                // Third attempt: Super Relaxed search (Track + Primary Artist) if multi-artist
                if (lyricsList.Count == 0 && (artistName.Contains('&') || artistName.Contains(','))) {
                    var primaryArtist = artistName.Split(new[] { '&', ',' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                    logger?.Log($"[LyricsClient] Relaxed search failed, trying primary artist: '{primaryArtist}'");
                    lyricsList = await FetchLyricsInternal(trackName, primaryArtist, null, null, cacheFile);
                }
                
                if (lyricsList.Count > 0) {
                    return lyricsList;
                }

            } catch (Exception ex) {
                logger?.Log($"Exception while fetching lyrics: {ex.Message}");
            }

            return new List<LrcLine>();
        }

        private async Task<List<LrcLine>> FetchLyricsInternal(string trackName, string artistName, string? albumName, int? duration, string? cacheFileToSave = null) {
             var query = HttpUtility.ParseQueryString(string.Empty);
             query["track_name"] = trackName;
             query["artist_name"] = artistName;
             if (albumName != null) query["album_name"] = albumName;
             if (duration != null) query["duration"] = duration.ToString();

             var url = $"{LRCLIB_API_URL}?{query}";
             
             try {
                 var response = await httpClient.GetAsync(url);
                 if (!response.IsSuccessStatusCode) {
                      return new List<LrcLine>();
                 }

                 var json = await response.Content.ReadAsStringAsync();
                 using (var doc = JsonDocument.Parse(json)) {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("syncedLyrics", out var syncedLyricsProp) && syncedLyricsProp.ValueKind == JsonValueKind.String) {
                         var rawLrc = syncedLyricsProp.GetString();
                         if (!string.IsNullOrEmpty(rawLrc)) {
                             // SUCCESS!
                             logger?.Log($"[LyricsClient] Synced lyrics found! Length: {rawLrc.Length}");

                             if (cacheFileToSave != null) {
                                 try {
                                     await System.IO.File.WriteAllTextAsync(cacheFileToSave, json);
                                     logger?.Log($"[LyricsClient] Saved to cache: {cacheFileToSave}");
                                 } catch (Exception ex) {
                                      logger?.Log($"[LyricsClient] Could not save cache: {ex.Message}");
                                 }
                             }
                             
                             return ParseLrc(rawLrc); 
                         }
                    }
                 }
             } catch (Exception ex) {
                 logger?.Log($"[LyricsClient] Internal fetch error: {ex.Message}");
             }
             return new List<LrcLine>();
        }

        private List<LrcLine> ParseLrc(string lrcContent) {
            var lines = new List<LrcLine>();
            var splitLines = lrcContent.Split('\n');
            
            foreach (var line in splitLines) {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                try {
                    var bracketCloseIndex = trimmed.IndexOf(']');
                    if (trimmed.StartsWith("[") && bracketCloseIndex > 1) {
                         var timePart = trimmed.Substring(1, bracketCloseIndex - 1);
                         var textPart = trimmed.Substring(bracketCloseIndex + 1).Trim();

                         var timeSpan = ParseTime(timePart);
                         lines.Add(new LrcLine { Time = timeSpan, Text = textPart });
                    }
                } catch {
                    // ignore malformed lines
                }
            }
            return lines;
        }

        private TimeSpan ParseTime(string timeStr) {
            var parts = timeStr.Split(':');
            var minutes = int.Parse(parts[0]);
            var secondsPart = parts[1];
            double seconds = double.Parse(secondsPart, System.Globalization.CultureInfo.InvariantCulture);
            
            return TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }
    }
}
