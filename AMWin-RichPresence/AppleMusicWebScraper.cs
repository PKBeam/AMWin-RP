using HtmlAgilityPack;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System;
using System.Drawing;

namespace AMWin_RichPresence {
    internal class AppleMusicWebScraper
    {
        private static readonly Regex DurationRegex = new Regex(@"[0-9]*:[0-9]{2}", RegexOptions.Compiled);
        private static readonly Regex ImageUrlRegex = new Regex(@"http\S*?(?= \d{2,3}w)", RegexOptions.Compiled);
        Logger? logger;
        string? lastFmApiKey;
        string songName;
        string songAlbum;
        string songArtist;
        string region;

        HtmlNode? cachedSongSearchResults;
        private async Task<HtmlNode?> SearchSongs() {
            if (cachedSongSearchResults == null) {
                var t = await _SearchSongs();
                if (t != null) {
                    cachedSongSearchResults = t;
                    logger?.Log($"[SearchSongs] Caching result for {songName}");
                } else {
                    logger?.Log($"[SearchSongs] No result found for {songName}");
                }
            } else {
                logger?.Log($"[SearchSongs] Using cached result for {songName}");
            }
            return cachedSongSearchResults;
        }

        HtmlNode? cachedSongSearchTopResults;
        private async Task<HtmlNode?> SearchTopResults() {
            if (cachedSongSearchTopResults == null) {
                var t = await _SearchTopResults();
                if (t != null) {
                    cachedSongSearchTopResults = t;
                    logger?.Log($"[SearchTopResults] Caching result for {songName}");
                } else {
                    logger?.Log($"[SearchTopResults] No result found for {songName}");
                }
            } else {
                logger?.Log($"[SearchTopResults] Using cached result for {songName}");
            }
            return cachedSongSearchTopResults;
        }

        public AppleMusicWebScraper(string songName, string songAlbum, string songArtist, string region, Logger? logger = null, string? lastFmApiKey = null) {
            this.logger = logger;
            this.lastFmApiKey = lastFmApiKey;
            this.songName = songName;
            this.songAlbum = songAlbum;
            this.songArtist = songArtist;
            this.region = region.ToLower();
        }
        private async Task<HtmlDocument> GetURL(string url, string? callingFunction = null) {
            // Apple Music web search doesn't like ampersands... even if they're HTML-escaped?
            var cleanUrl = HttpUtility.HtmlEncode(url.Replace("&", " "));
            logger?.Log($"[{callingFunction ?? "GetURL"}] HTTP GET for {cleanUrl}");
            var stopwatch = Stopwatch.StartNew();
            var res = await Constants.HttpClient.GetStringAsync(cleanUrl);
            stopwatch.Stop();
            logger?.Log($"[{callingFunction ?? "GetURL"}] HTTP GET for {cleanUrl} took {stopwatch.ElapsedMilliseconds}ms");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(res);
            return doc;
        }
        private async Task<JsonDocument> GetURLJson(string url, string? callingFunction = null) {
            logger?.Log($"[{callingFunction ?? "GetURL"}] HTTP GET for {url}");
            var stopwatch = Stopwatch.StartNew();
            var res = await Constants.HttpClient.GetStringAsync(url);
            stopwatch.Stop();
            logger?.Log($"[{callingFunction ?? "GetURL"}] HTTP GET for {url} took {stopwatch.ElapsedMilliseconds}ms");
            return JsonDocument.Parse(res);
        }

        // Apple Music web search functions
        private async Task<HtmlNode?> _SearchSongs() {

            // search on the Apple Music website for the song
            var searchTerm = Uri.EscapeDataString($"{songName} {songAlbum} {songArtist}");
            var url = $"https://music.apple.com/{region}/search?term={searchTerm}";
            HtmlDocument doc = await GetURL(url, "SearchSongs");

            try {
                // scrape search results for "Songs" section
                var list = doc.DocumentNode
                    .Descendants("div")
                    .First(x => x.HasClass("desktop-search-page"))
                    .Descendants("ul")
                    .First(x => x.HasClass("shelf-grid__list--grid-type-TrackLockupsShelf"))
                    .ChildNodes
                    .Where(x => x.Name == "li");

                // try each result until we find one that looks correct
                foreach (var result in list) {

                    if (result.InnerHtml == "") {
                        continue;
                    }

                    var searchResultTitle = result
                            .Descendants("li")
                            .First(x => x.Attributes["data-testid"].Value == "track-lockup-title")
                            .Descendants("a")
                            .First()
                            .InnerHtml;

                    var searchResultSubtitles = result
                        .Descendants("span")
                        .Where(x => x.Attributes.Contains("data-testid") && x.Attributes["data-testid"].Value == "track-lockup-subtitle");

                    var searchResultSubtitlesList = new List<string>() { };
                    foreach (var span in searchResultSubtitles) {
                        searchResultSubtitlesList.Add(span.Descendants("span").First().InnerHtml);
                    }
                    var searchResultSubtitle = string.Join(", ", searchResultSubtitlesList);

                    // need to decode html to avoid instances like "&amp;" instead of "&"
                    searchResultTitle = HttpUtility.HtmlDecode(searchResultTitle);
                    searchResultSubtitle = HttpUtility.HtmlDecode(searchResultSubtitle);

                    // check that the result actually is the song
                    // (Apple Music web search's "Song" section replaces ampersands with commas in the artist list)
                    if (searchResultTitle == songName && searchResultSubtitle == songArtist.Replace(" & ", ", ")) {
                        return result;
                    }
                }
                return null;
            } catch (Exception ex) {
                logger?.Log($"[SearchSongs] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<HtmlNode?> _SearchTopResults() {
            // search on the Apple Music website for the song
            var searchTerm = Uri.EscapeDataString($"{songName} {songAlbum} {songArtist}");
            var url = $"https://music.apple.com/{region}/search?term={searchTerm}";
            HtmlDocument doc = await GetURL(url, "SearchTopResults");

            try {
                // scrape search results for "Top Results" section
                var list = doc.DocumentNode
                    .Descendants("ul")
                    .First(x => x.Attributes["class"].Value.Contains("grid--top-results"))
                    .Descendants("li")
                    .Where(x => x.Attributes.Contains("data-testid") && x.Attributes["data-testid"].Value == "grid-item")
                    .ToList();

                // try each result until we find one that looks correct
                foreach (var result in list) {

                    var searchResultTitle = result
                        .Descendants("li")
                        .First(x => x.Attributes["data-testid"].Value == "top-search-result-title")
                        .Descendants("span")
                        .First()
                        .InnerHtml;

                    var searchResultSubtitle = result
                        .Descendants("li")
                        .First(x => x.Attributes["data-testid"].Value == "top-search-result-subtitle")
                        .InnerHtml;

                    // need to decode html to avoid instances like "&amp;" instead of "&"
                    searchResultTitle = HttpUtility.HtmlDecode(searchResultTitle);
                    searchResultSubtitle = HttpUtility.HtmlDecode(searchResultSubtitle);

                    // check that the result actually is the song
                    if (searchResultTitle.ToLower() == songName.ToLower() && searchResultSubtitle.ToLower() == $"Song · {songArtist}".ToLower()) {
                        return result;
                    }
                }
                return null;
            } catch (Exception ex) {
                logger?.Log($"[SearchTopResults] An exception occurred: {ex}");
                return null;
            }
        }

        // Get list of artists for a song
        // -----------------------------------------------
        // Supported APIs: Apple Music web search
        public async Task<List<string>> GetArtistList() {
            try {
                var result = await SearchSongs();
                if (result != null) {
                    /*
                    var searchResultUrl = result
                        .Descendants("li")
                        .First(x => x.Attributes["class"].Value.Contains("track-lockup__title"))
                        .Descendants("a")
                        .First()
                        .Attributes["href"]
                        .Value;
                    */

                    var searchResultSubtitles = result
                        .Descendants("span")
                        .Where(x => x.Attributes.Contains("data-testid") && x.Attributes["data-testid"].Value == "track-lockup-subtitle");

                    var searchResultSubtitlesList = new List<string>() { };
                    foreach (var span in searchResultSubtitles) {
                        searchResultSubtitlesList.Add(span.Descendants("span").First().InnerHtml);
                    }

                    return searchResultSubtitlesList;
                }
                return [];
            } catch (Exception ex) {
                logger?.Log($"[GetArtistList] An exception occurred: {ex}");
                return [];
            }
        }
        
        // Get song URL
        // -----------------------------------------------
        // Supported APIs: Apple Music web search
        public async Task<string?> GetSongUrl()
        {
            try {
                return await GetSongUrlAppleMusic();
            } catch (Exception ex) {
                logger?.Log($"[GetMusicUrl] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<string?> GetSongUrlAppleMusic()
        {
            try {
                var result = await SearchSongs();
                if (result != null) {
                    return GetSongUrl(result);
                }
                
                result = await SearchTopResults();
                if (result != null) {
                    return GetSongUrl(result);
                }
                return null;
            } catch (Exception ex) {
                logger?.Log($"[GetMusicUrlAppleMusic] An exception occurred: {ex}");
                return null;
            }
        }

        // Get album artwork image
        // -----------------------------------------------
        // Supported APIs: Last.FM, Apple Music web search
        public async Task<string?> GetAlbumArtUrl() {
            try {
                var lastFmImg = (lastFmApiKey == null || lastFmApiKey == "") ? null : await GetAlbumArtUrlLastFm();
                if (lastFmApiKey != null && lastFmImg == null) {
                    logger?.Log($"[GetAlbumArtUrl] LastFM lookup failed, falling back to Apple Music Web");
                }
                return lastFmImg ?? await GetAlbumArtUrlAppleMusic();
            } catch (Exception ex) {
                logger?.Log($"[GetAlbumArtUrl] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<string?> GetAlbumArtUrlLastFm() {
            var url = $"http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={lastFmApiKey}&artist={Uri.EscapeDataString(songArtist)}&album={Uri.EscapeDataString(AlbumCleaner.CleanAlbumName(songAlbum))}&format=json";
            var j = await GetURLJson(url, "GetAlbumArtUrlLastFm");
            var imgs = j.RootElement.GetProperty("album").GetProperty("image");
            foreach (var img in imgs.EnumerateArray()) {
                if (img.GetProperty("size").ToString() == "mega") {
                    var imgUrl = img.GetProperty("#text").ToString();
                    return imgUrl == "" ? null : imgUrl;
                }
            }
            return null;
        }
        private async Task<string?> GetAlbumArtUrlAppleMusic() {
            try {
                // try searching in "Songs" section
                var result = await SearchSongs();
                if (result != null) {
                    return GetLargestImageUrl(result);
                }

                // now search results for "Top Results" section
                result = await SearchTopResults();
                if (result != null) {
                    return GetLargestImageUrl(result);
                }

                // TODO: search in "Albums" section?
                return null;
            } catch (Exception ex) {
                logger?.Log($"[GetAlbumArtUrlAppleMusic] An exception occurred: {ex}");
                return null;
            }
        }
        private string GetLargestImageUrl(HtmlNode nodeWithSource) {
            var imgSources = nodeWithSource
                .Descendants("source")
                .Where(x => x.Attributes["type"].Value == "image/jpeg")
                .ToList();
            var imgUrls = imgSources[0].Attributes["srcset"].Value;

            return ImageUrlRegex.Matches(imgUrls).Last().Value;
        }

        private string? GetSongUrl(HtmlNode nodeWithSource) {
            var musicLinkNode = nodeWithSource.SelectSingleNode(".//a[@data-testid='click-action']");
            return musicLinkNode?.GetAttributeValue("href", "");
        }

        // Get song duration
        // -----------------------------------------------
        // Supported APIs: Last.FM, Apple Music web search
        public async Task<string?> GetSongDuration() {
            try {
                var lastFmDur = (lastFmApiKey == null || lastFmApiKey == "") ? null : await GetSongDurationLastFm();
                if (lastFmApiKey != null && lastFmDur == null) {
                    logger?.Log($"[GetSongDuration] LastFM lookup failed, falling back to Apple Music Web");
                }
                return lastFmDur ?? await GetSongDurationAppleMusic();
            } catch (Exception ex) {
                logger?.Log($"[GetSongDuration] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<string?> GetSongDurationLastFm() {
            var url = $"http://ws.audioscrobbler.com/2.0/?method=track.getinfo&api_key={lastFmApiKey}&artist={Uri.EscapeDataString(songArtist)}&track={Uri.EscapeDataString(songName)}&format=json";
            var j = await GetURLJson(url, "GetSongDurationLastFm");
            var track = j.RootElement.GetProperty("track");
            try {
                var dur = int.Parse(track.GetProperty("duration").ToString()) / 1000;
                return dur == 0 ? null : $"{dur / 60}:{$"{dur % 60}".PadLeft(2, '0')}";
            } catch (Exception ex) {
                logger?.Log($"[GetSongDurationLastFm] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<string?> GetSongDurationAppleMusic() {
            try {
                var result = await SearchSongs();
                if (result != null) {
                    var searchResultUrl = result
                        .Descendants("li")
                        .First(x => x.Attributes["class"].Value.Contains("track-lockup__title"))
                        .Descendants("a")
                        .First()
                        .Attributes["href"]
                        .Value;

                    return await GetSongDurationFromAlbumPage(searchResultUrl);
                }
                return null;
            } catch (Exception ex) {
                logger?.Log($"[GetSongDurationAppleMusic] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<string?> GetSongDurationFromAlbumPage(string url) {
            HtmlDocument doc = await GetURL(url, "GetSongDurationFromAlbumPage");
            try {
                var desc = doc.DocumentNode
                    .Descendants("meta")
                    .First(x => x.Attributes.Contains("name") && x.Attributes["name"].Value == "description");

                var str = desc.Attributes["content"].Value;
                var duration = DurationRegex.Matches(str).First().Value;

                // check that the result actually is the song
                if (HttpUtility.HtmlDecode(str).Contains(songName)) {
                    return duration;
                }
                return null;
            } catch (Exception ex) {
                logger?.Log($"[GetSongDurationFromAlbumPage] An exception occurred: {ex}");
                return null;
            }
        }
    }
}
