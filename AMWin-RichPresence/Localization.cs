using System;
using System.Collections.Generic;

namespace AMWin_RichPresence {
    public static class Localization {
        public static string CurrentRegion => Properties.Settings.Default.AppleMusicRegion.ToLower();
        public static bool IsTurkish => CurrentRegion == "tr";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>> {
            ["tr"] = new Dictionary<string, string> {
                // Headers
                ["Discord settings"] = "Discord ayarları",
                ["Lyrics settings"] = "Şarkı sözü ayarları",
                ["Scrobbling settings"] = "Scrobbling ayarları",
                ["Last.FM"] = "Last.FM",
                ["ListenBrainz"] = "ListenBrainz",

                // Checkboxes
                ["Run when Windows starts"] = "Windows başladığında çalıştır",
                ["Check for updates on startup"] = "Açılışta güncellemeleri kontrol et",
                ["Treat composer as artist"] = "Besteciyi sanatçı olarak kabul et",
                ["Enable Discord RP"] = "Discord RP'yi etkinleştir",
                ["Enable cover images"] = "Kapak resimlerini göster",
                ["Enable album info"] = "Albüm bilgisini göster",
                ["RP when music paused"] = "Müzik duraklatıldığında RP'yi kapatma",
                ["Apple Music icon in status"] = "Durumda Apple Music ikonunu göster",
                ["Enable sync lyrics"] = "Senkronize şarkı sözlerini etkinleştir",
                ["Extend lyrics line"] = "Söz satırını sonraki söze kadar uzat",
                ["Clean album name"] = "Albüm adını temizle",
                ["Scrobble primary artist"] = "Sadece ana sanatçıyı scrobble et",
                ["Prefer song duration from Apple Music Web"] = "Şarkı süresini Apple Music Web'den almayı tercih et",
                ["Enable Last.FM"] = "Last.FM'i etkinleştir",
                ["Enable ListenBrainz"] = "ListenBrainz'i etkinleştir",

                // Labels
                ["Apple Music region"] = "Apple Music bölgesi",
                ["Rich Presence display"] = "Discord Durum görünümü",
                ["Max time before scrobble (sec)"] = "Scrobble öncesi max süre (sn)",
                ["User token"] = "Kullanıcı tokeni",
                ["API Key"] = "API Anahtarı",
                ["API Secret"] = "API Gizli Anahtarı",
                ["Username"] = "Kullanıcı adı",
                ["Password"] = "Şifre",

                // Buttons
                ["Save Credentials"] = "Kimlik Bilgilerini Kaydet",
                ["Open saved lyrics"] = "Kaydedilen sözleri aç",
                ["Delete saved lyrics"] = "Kaydedilen sözleri sil",

                // ComboBox Items
                ["Artist Name"] = "Sanatçı Adı",
                ["Apple Music"] = "Apple Music",
                ["Song Name"] = "Şarkı Adı",

                // Messages
                ["Are you sure you want to delete all saved lyrics?"] = "Tüm kaydedilen sözleri silmek istediğinize emin misiniz?",
                ["Delete Saved Lyrics"] = "Kaydedilen Sözleri Sil",
                ["All saved lyrics have been deleted."] = "Tüm kaydedilen sözler silindi.",
                ["Lyrics Deleted"] = "Sözler Silindi",
                ["Could not delete lyrics: "] = "Sözler silinemedi: ",
                ["Error"] = "Hata",
                ["No saved lyrics found."] = "Kaydedilen söz bulunamadı.",
                ["Information"] = "Bilgi",
                ["The Last.FM credentials were successfully authenticated."] = "Last.FM kimlik bilgileri başarıyla doğrulandı.",
                ["Last.FM Authentication"] = "Last.FM Doğrulaması",
                ["The Last.FM credentials could not be authenticated. Please make sure you have entered the correct username and password, and that your account is not currently locked."] = "Last.FM kimlik bilgileri doğrulanamadı. Lütfen kullanıcı adı ve şifrenizi doğru girdiğinizden ve hesabınızın kilitli olmadığından emin olun.",
                ["The ListenBrainz credentials were successfully authenticated."] = "ListenBrainz kimlik bilgileri başarıyla doğrulandı.",
                ["ListenBrainz Authentication"] = "ListenBrainz Doğrulaması",
                ["The ListenBrainz credentials could not be authenticated. Please make sure you have entered the correct user token."] = "ListenBrainz kimlik bilgileri doğrulanamadı. Lütfen kullanıcı tokeninizi doğru girdiğinizden emin olun.",
                ["Apple Music'de Dinle"] = "Apple Music'de Dinle", // Default for TR
                ["Listen on Apple Music"] = "Listen on Apple Music"
            }
        };

        public static string Get(string key) {
            string region = CurrentRegion;
            if (Translations.ContainsKey(region) && Translations[region].ContainsKey(key)) {
                return Translations[region][key];
            }
            return key; // Fallback to original English key
        }
    }
}
