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

namespace AMWin_RichPresence {
    internal class AppleMusicWebScraper {
        Logger? logger;
        string lastFmApiKey;
        public AppleMusicWebScraper(Logger? logger = null, string? lastFmApiKey = null) {
            this.logger = logger;
            this.lastFmApiKey = lastFmApiKey ?? "";
            logger?.Log($"WebScraper created with Last FM API key {lastFmApiKey}");
        }
        private async Task<HtmlDocument> GetURL(string url) {
            var client = new HttpClient();
            // Apple Music web search doesn't like ampersands... even if they're HTML-escaped?
            var cleanUrl = HttpUtility.HtmlEncode(url.Replace("&", " "));
            logger?.Log($"Starting HTTP GET for {cleanUrl}");
            var stopwatch = Stopwatch.StartNew();
            var res = await client.GetStringAsync(cleanUrl);
            stopwatch.Stop();
            logger?.Log($"HTTP GET for {cleanUrl} took {stopwatch.ElapsedMilliseconds}ms");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(res);
            return doc;
        }
        private async Task<JsonDocument> GetURLJson(string url) {
            var client = new HttpClient();
            logger?.Log($"Starting HTTP GET for {url}");
            var stopwatch = Stopwatch.StartNew();
            var res = await client.GetStringAsync(url);
            stopwatch.Stop();
            logger?.Log($"HTTP GET for {url} took {stopwatch.ElapsedMilliseconds}ms");
            return JsonDocument.Parse(res);
        }

        // Apple Music web search functions
        private async Task<HtmlNode?> SearchTopResults(string songName, string songAlbum, string songArtist) {
            // search on the Apple Music website for the song
            var searchTerm = Uri.EscapeDataString($"{songName} {songAlbum} {songArtist}");
            var url = $"https://music.apple.com/us/search?term={searchTerm}";
            HtmlDocument doc = await GetURL(url);

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
        private async Task<HtmlNode?> SearchSongs(string songName, string songAlbum, string songArtist) {

            // search on the Apple Music website for the song
            var searchTerm = Uri.EscapeDataString($"{songName} {songAlbum} {songArtist}");
            var url = $"https://music.apple.com/us/search?term={searchTerm}";
            HtmlDocument doc = await GetURL(url);

            try {
                // scrape search results for "Songs" section
                var list = doc.DocumentNode
                    .Descendants("div")
                    .First(x => x.Attributes.Contains("aria-label") && x.Attributes["aria-label"].Value == "Songs")
                    .Descendants("ul")
                    .First(x => x.Attributes["data-testid"].Value == "shelf-item-list")
                    .ChildNodes
                    .Where(x => x.Name == "li");

                // try each result until we find one that looks correct
                foreach (var result in list) {

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

        // Get list of artists for a song
        // -----------------------------------------------
        // Supported APIs: Apple Music web search
        public async Task<List<string>> GetArtistList(string songName, string songAlbum, string songArtist) {
            try {
                var result = await SearchSongs(songName, songAlbum, songArtist);
                if (result != null) {
                    var searchResultUrl = result
                        .Descendants("li")
                        .First(x => x.Attributes["class"].Value.Contains("track-lockup__title"))
                        .Descendants("a")
                        .First()
                        .Attributes["href"]
                        .Value;

                    var searchResultSubtitles = result
                        .Descendants("span")
                        .Where(x => x.Attributes.Contains("data-testid") && x.Attributes["data-testid"].Value == "track-lockup-subtitle");

                    var searchResultSubtitlesList = new List<string>() { };
                    foreach (var span in searchResultSubtitles) {
                        searchResultSubtitlesList.Add(span.Descendants("span").First().InnerHtml);
                    }

                    return searchResultSubtitlesList;
                }
                return new();
            } catch (Exception ex) {
                logger?.Log($"[GetArtistList] An exception occurred: {ex}"); 
                return new();
            }
        }

        // Get album artwork image
        // -----------------------------------------------
        // Supported APIs: Last.FM, Apple Music web search
        public async Task<string?> GetAlbumArtUrl(string songName, string songAlbum, string songArtist) {
            try {
                var lastFmImg = (lastFmApiKey == "") ? null : await GetAlbumArtUrlLastFm(songAlbum, songArtist);
                if (lastFmImg == null) {
                    logger?.Log($"[GetAlbumArtUrl] LastFM lookup failed, falling back to Apple Music Web");
                }
                return lastFmImg ?? await GetAlbumArtUrlAppleMusic(songName, songAlbum, songArtist);
            } catch (Exception ex) { 
                logger?.Log($"[GetAlbumArtUrl] An exception occurred: {ex}"); 
                return null; 
            }
        }
        private async Task<string?> GetAlbumArtUrlLastFm(string songAlbum, string songArtist) {
            var j = await GetURLJson($"http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={lastFmApiKey}&artist={Uri.EscapeDataString(songArtist)}&album={Uri.EscapeDataString(AppleMusicScrobbler.CleanAlbumName(songAlbum))}&format=json");
            var imgs = j.RootElement.GetProperty("album").GetProperty("image");
            foreach (var img in imgs.EnumerateArray()) {
                if (img.GetProperty("size").ToString() == "mega") {
                    var imgUrl = img.GetProperty("#text").ToString();
                    return imgUrl == "" ? null : imgUrl;
                }
            }
            return null;
        }
        private async Task<string?> GetAlbumArtUrlAppleMusic(string songName, string songAlbum, string songArtist) {
            try {
                // scrape search results for "Top Results" section
                var result = await SearchTopResults(songName, songAlbum, songArtist);
                if (result != null) { 
                    return GetLargestImageUrl(result);
                }

                // now try searching in "Songs" section 
                result = await SearchSongs(songName, songAlbum, songArtist);
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

            return new Regex(@"http\S*?(?= \d{2,3}w)").Matches(imgUrls).Last().Value;
        }

        // Get song duration
        // -----------------------------------------------
        // Supported APIs: Last.FM, Apple Music web search
        public async Task<string?> GetSongDuration(string songName, string songAlbum, string songArtist) {
            try {
                var lastFmDur = (lastFmApiKey == "") ? null : await GetSongDurationLastFm(songName, songArtist);
                if (lastFmDur == null) {
                    logger?.Log($"[GetSongDuration] LastFM lookup failed, falling back to Apple Music Web");
                }
                return lastFmDur ?? await GetSongDurationAppleMusic(songName, songAlbum, songArtist);
            } catch (Exception ex) {
                logger?.Log($"[GetSongDuration] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<string?> GetSongDurationLastFm(string songName, string songArtist) {
            var url = $"http://ws.audioscrobbler.com/2.0/?method=track.getinfo&api_key={lastFmApiKey}&artist={Uri.EscapeDataString(songArtist)}&track={Uri.EscapeDataString(songName)}&format=json";
            var j = await GetURLJson(url);
            var track = j.RootElement.GetProperty("track");
            try {
                var dur = int.Parse(track.GetProperty("duration").ToString()) / 1000;
                return dur == 0 ? null : $"{dur / 60}:{$"{dur % 60}".PadLeft(2, '0')}";
            } catch (Exception ex) {
                logger?.Log($"[GetSongDurationLastFm] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<string?> GetSongDurationAppleMusic(string songName, string songAlbum, string songArtist) {
            try {
                var result = await SearchSongs(songName, songAlbum, songArtist);
                if (result != null) {
                    var searchResultUrl = result
                        .Descendants("li")
                        .First(x => x.Attributes["class"].Value.Contains("track-lockup__title"))
                        .Descendants("a")
                        .First()
                        .Attributes["href"]
                        .Value;

                    return await GetSongDurationFromAlbumPage(searchResultUrl, songName);
                }    
                return null;
            } catch (Exception ex) {
                logger?.Log($"[GetSongDurationAppleMusic] An exception occurred: {ex}");
                return null;
            }
        }
        private async Task<string?> GetSongDurationFromAlbumPage(string url, string songName) {
            HtmlDocument doc = await GetURL(url);
            try {
                var list = doc.DocumentNode
                        .Descendants("div")
                        .First(x => x.Attributes.Contains("data-testid") && x.Attributes["data-testid"].Value == "content-container")
                        .Descendants("div")
                        .Where(x => x.Attributes.Contains("style") && x.Attributes["style"].Value == "display: contents;")
                        .ToList();

                // try each result until we find one that looks correct
                foreach (var result in list) {
                    var songTitle = result
                            .Descendants("a")
                            .First()
                            .InnerHtml;

                    var duration = result
                        .Descendants("time")
                        .First()
                        .InnerHtml;

                    // check that the result actually is the song
                    if (HttpUtility.HtmlDecode(songTitle) == songName) {
                        return duration;
                    }
                }
                return null;
            } catch (Exception ex) {
                logger?.Log($"[GetSongDurationFromAlbumPage] An exception occurred: {ex}");
                return null;
            }
        }
    }
}
