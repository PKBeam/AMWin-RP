using HtmlAgilityPack;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Web;

namespace AMWin_RichPresence {
    internal class AppleMusicWebScraper {
        private static int TimeStringToSec(string s) {
            var split = s.Split(":");
            return 60 * int.Parse(split[0]) + int.Parse(split[1]);
        }
        private static HtmlDocument GetURL(string url) {
            var client = new HttpClient();
            var res = client.GetStringAsync(url).Result;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(res);
            return doc;
        } 
        public static string? GetAlbumArtUrl(string songName, string songAlbum, string songArtist) {

            // search on the Apple Music website for the song
            var url = $"https://music.apple.com/us/search?term={songName} {songAlbum} {songArtist}";
            HtmlDocument doc = GetURL(url);

            try {

                // scrape search results for "Top Results" section
                // TODO: scrape rest of search page too
                var list = doc.DocumentNode
                    .Descendants("ul")
                    .Where(x => x.Attributes["class"].Value.Contains("grid--top-results"))
                    .First();

                // try each result until we find one that looks correct
                foreach (var result in list.ChildNodes) {

                    var imgSources = result
                        .Descendants("source")
                        .Where(x => x.Attributes["type"].Value == "image/jpeg")
                        .ToList();

                    var x = imgSources[0].Attributes["srcset"].Value;

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
                    if (searchResultTitle == songName && searchResultSubtitle == $"Song · {songArtist}") {
                        return x.Split(' ')[0];
                    }
                }
                return null;
            } catch {
                return null;
            }
        }
        private static string? GetSongDurationFromAlbumPage(string url, string songName) {
            HtmlDocument doc = GetURL(url);
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
                    if (songTitle == songName) {
                        return duration; 
                    }
                }
                return null;
            } catch {
                return null;
            }
        }
        public static string? GetSongDuration(string songName, string songAlbum, string songArtist) {

            // search on the Apple Music website for the song
            var url = $"https://music.apple.com/us/search?term={songName} {songAlbum} {songArtist}";
            HtmlDocument doc = GetURL(url);

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

                    var searchResultUrl = result
                        .Descendants("li")
                        .First(x => x.Attributes["class"].Value.Contains("track-lockup__title"))
                        .Descendants("a")
                        .First()
                        .Attributes["href"]
                        .Value;

                    // need to decode html to avoid instances like "&amp;" instead of "&"
                    searchResultTitle = HttpUtility.HtmlDecode(searchResultTitle);
                    searchResultSubtitle = HttpUtility.HtmlDecode(searchResultSubtitle);

                    // check that the result actually is the song
                    // (Apple Music web search's "Song" section replaces ampersands with commas in the artist list)
                    if (searchResultTitle == songName && searchResultSubtitle == songArtist.Replace(" & ", ", ")) {
                        return GetSongDurationFromAlbumPage(searchResultUrl, songName);
                    }
                }
                return null;
            } catch {
                return null;
            }
        }
    }
}
