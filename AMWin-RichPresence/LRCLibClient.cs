using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace AMWin_RichPresence {
    internal class LyricResult {
        public List<LyricLine>? Lyrics { get; set; }
        public int? Duration { get; set; }
    }

    internal class LyricLine {
        public TimeSpan Time { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    internal class LRCLibClient {
        private static readonly Regex LrcRegex = new Regex(@"^\[(?<min>\d+):(?<sec>\d+\.\d+)\](?<text>.*)$", RegexOptions.Compiled);
        private Logger? logger;
        private static string CacheFolder => Path.Combine(Constants.AppDataFolder, "LyricCache");

        public LRCLibClient(Logger? logger = null) {
            this.logger = logger;
            if (!Directory.Exists(CacheFolder)) {
                Directory.CreateDirectory(CacheFolder);
            }
        }

        private string GetCacheFileName(string title, string artist) {
            // Create a human-readable filename: Artist - Title.json
            var fileName = $"{artist} - {title}.json";
            
            // Remove illegal characters for Windows filenames
            foreach (char c in Path.GetInvalidFileNameChars()) {
                fileName = fileName.Replace(c, '_');
            }
            
            return fileName;
        }

        public async Task<LyricResult?> GetSyncedLyrics(string title, string artist, int? durationSeconds = null) {
            var result = await SearchAndCacheLyrics(title, artist, durationSeconds);
            if (result != null) return result;

            // Fallback: try with only the primary artist if the full list fails
            string primaryArtist = GetPrimaryArtist(artist);
            if (primaryArtist != artist) {
                logger?.Log($"[LRCLib] Full artist search failed. Retrying with primary artist: {primaryArtist}");
                return await SearchAndCacheLyrics(title, primaryArtist, durationSeconds);
            }

            return null;
        }

        private async Task<LyricResult?> SearchAndCacheLyrics(string title, string artist, int? durationSeconds) {
            var cacheFile = Path.Combine(CacheFolder, GetCacheFileName(title, artist));
            
            // Check local cache first
            if (File.Exists(cacheFile)) {
                try {
                    logger?.Log($"[LRCLib] Loading lyrics from local cache: {title} - {artist}");
                    var cachedJson = await File.ReadAllTextAsync(cacheFile);
                    return JsonSerializer.Deserialize<LyricResult>(cachedJson);
                } catch (Exception ex) {
                    // Try to handle legacy cache format (List<LyricLine>)
                    try {
                        var cachedJson = await File.ReadAllTextAsync(cacheFile);
                        var lyrics = JsonSerializer.Deserialize<List<LyricLine>>(cachedJson);
                        return new LyricResult { Lyrics = lyrics };
                    } catch {
                        logger?.Log($"[LRCLib] Error reading cache file: {ex.Message}");
                    }
                }
            }

            try {
                var query = HttpUtility.UrlEncode($"{title} {artist}");
                var url = $"https://lrclib.net/api/search?q={query}";
                
                logger?.Log($"[LRCLib] Searching for lyrics: {url}");
                var response = await Constants.HttpClient.GetStringAsync(url);
                var results = JsonDocument.Parse(response).RootElement;

                if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0) {
                    return null;
                }

                // Look for the best match (syncedLyrics is present and duration matches if provided)
                foreach (var item in results.EnumerateArray()) {
                    if (item.TryGetProperty("syncedLyrics", out var syncedLyricsProp) && !string.IsNullOrEmpty(syncedLyricsProp.GetString())) {
                        
                        if (durationSeconds != null && item.TryGetProperty("duration", out var durProp)) {
                            double matchedApiDuration = durProp.GetDouble();
                            // allow 5 seconds difference
                            if (Math.Abs(matchedApiDuration - durationSeconds.Value) > 5) {
                                continue;
                            }
                        }

                        var syncedLyrics = syncedLyricsProp.GetString()!;
                        var parsedLyrics = ParseLrc(syncedLyrics);
                        int? apiDuration = item.TryGetProperty("duration", out var dProp) ? (int?)dProp.GetDouble() : null;

                        var result = new LyricResult {
                            Lyrics = parsedLyrics,
                            Duration = apiDuration
                        };

                        // Save to cache
                        if (parsedLyrics != null && parsedLyrics.Count > 0) {
                            try {
                                var json = JsonSerializer.Serialize(result);
                                await File.WriteAllTextAsync(cacheFile, json);
                                logger?.Log($"[LRCLib] Saved lyrics to local cache: {title} - {artist}");
                            } catch (Exception ex) {
                                logger?.Log($"[LRCLib] Error saving cache file: {ex.Message}");
                            }
                        }

                        return result;
                    }
                }

                return null;
            } catch (Exception ex) {
                logger?.Log($"[LRCLib] Error fetching lyrics: {ex.Message}");
                return null;
            }
        }

        private static string GetPrimaryArtist(string artist) {
            if (string.IsNullOrWhiteSpace(artist)) return artist;
            // Split by common separators and return the first part
            var separators = new[] { " & ", " , ", ",", " feat. ", " ft. ", " / " };
            string primary = artist;
            foreach (var sep in separators) {
                int index = primary.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
                if (index > 0) {
                    primary = primary.Substring(0, index);
                }
            }
            return primary.Trim();
        }

        private List<LyricLine> ParseLrc(string lrcContent) {
            var lines = new List<LyricLine>();
            var rawLines = lrcContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in rawLines) {
                var match = LrcRegex.Match(rawLine);
                if (match.Success) {
                    var min = int.Parse(match.Groups["min"].Value);
                    var sec = double.Parse(match.Groups["sec"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var text = match.Groups["text"].Value.Trim();

                    lines.Add(new LyricLine {
                        Time = TimeSpan.FromMinutes(min) + TimeSpan.FromSeconds(sec),
                        Text = text
                    });
                }
            }

            return lines.OrderBy(l => l.Time).ToList();
        }

        public static string GetCurrentLyric(List<LyricLine>? lyrics, TimeSpan currentTime) {
            if (lyrics == null || lyrics.Count == 0) return string.Empty;

            LyricLine? current = null;
            LyricLine? nextNonEmpty = null;
            int currentIndex = -1;

            for (int i = 0; i < lyrics.Count; i++) {
                if (lyrics[i].Time <= currentTime) {
                    current = lyrics[i];
                    currentIndex = i;
                } else {
                    break;
                }
            }

            if (current == null) return string.Empty;

            // Find the next meaningful line
            if (currentIndex != -1) {
                for (int i = currentIndex + 1; i < lyrics.Count; i++) {
                    if (!string.IsNullOrWhiteSpace(lyrics[i].Text)) {
                        nextNonEmpty = lyrics[i];
                        break;
                    }
                }
            }

            var timeSinceLineStarted = currentTime - current.Time;
            bool isExtendOn = AMWin_RichPresence.Properties.Settings.Default.ExtendLyricsLine;

            // If we are on an empty line (instrumental/gap)
            if (string.IsNullOrWhiteSpace(current.Text)) {
                if (isExtendOn && nextNonEmpty != null) {
                    return nextNonEmpty.Text; // Skip to next lyric early
                }
                return string.Empty;
            }

            // If the current line has been showing for too long (e.g. > 8 seconds)
            // AND there's a significant gap until the next line
            if (timeSinceLineStarted.TotalSeconds > 8 && nextNonEmpty != null) {
                if (isExtendOn) {
                    return nextNonEmpty.Text; // Jump to next lyric early
                } else {
                    return string.Empty; // Just clear it, singer is likely done with this line
                }
            }

            return current.Text;
        }
    }
}
