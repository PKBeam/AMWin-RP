using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AMWin_RichPresence {
    internal class AppleMusicInfo: IEquatable<AppleMusicInfo> {
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
        public string? ArtistUrl = null;
        public int? CurrentTime = null;
        public List<LyricLine>? SyncedLyrics = null;
        public bool LyricsSearched = false;

        public AppleMusicInfo(string songName, string songSubTitle, string songAlbum, string songArtist) {
            this.SongName = Sanitize(songName);
            this.SongSubTitle = Sanitize(songSubTitle);
            this.SongAlbum = Sanitize(songAlbum);
            this.SongArtist = Sanitize(songArtist);
        }

        private static string Sanitize(string s) {
            if (string.IsNullOrEmpty(s)) return s;
            // Remove common invisible/formatting characters used by Apple Music
            return Regex.Replace(s, @"[\u200B-\u200F\u202A-\u202E]", "").Trim();
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

}
