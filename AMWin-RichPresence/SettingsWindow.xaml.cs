using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Meziantou.Framework.Win32;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        private bool amRegionValid { 
            get { return Constants.ValidAppleMusicRegions.Contains(AppleMusicRegion.Text.ToLower()); }
        }
        public SettingsWindow() {
            InitializeComponent();
            TextBlock_VersionString.Text = Constants.ProgramVersion;
            AppleMusicRegion.Text = Properties.Settings.Default.AppleMusicRegion;
            LastfmPassword.Password = GetLastFMPassword();
            LocalizeUI();
        }

        private void LocalizeUI() {
            bool isTurkish = Properties.Settings.Default.AppleMusicRegion.Equals("tr", StringComparison.OrdinalIgnoreCase);
            
            this.Title = isTurkish ? "AMWin-RichPresence Ayarlar" : "AMWin-RichPresence Settings";

            CheckBox_RunOnStartup.Content = isTurkish ? "Windows başladığında çalıştır" : "Run when Windows starts";
            CheckBox_CheckForUpdatesOnStartup.Content = isTurkish ? "Açılışta güncellemeleri kontrol et" : "Check for updates on startup";
            CheckBox_ClassicalComposerAsArtist.Content = isTurkish ? "Besteciyi sanatçı olarak kabul et" : "Treat composer as artist";
            
            // Find and translate TextBlocks in the main stack
            var scrollViewer = (ScrollViewer)((Border)((DockPanel)((Grid)this.Content).Children[0]).Children[2]).Child;
            var mainStack = (StackPanel)scrollViewer.Content;
            
            foreach (var child in mainStack.Children) {
                if (child is TextBlock tb) {
                    if (tb.Text == "Discord settings" || tb.Text == "Discord ayarları") tb.Text = isTurkish ? "Discord ayarları" : "Discord settings";
                    else if (tb.Text == "Lyrics settings" || tb.Text == "Şarkı sözü ayarları") tb.Text = isTurkish ? "Şarkı sözü ayarları" : "Lyrics settings";
                    else if (tb.Text == "Scrobbling settings" || tb.Text == "Scrobbling ayarları") tb.Text = isTurkish ? "Scrobbling ayarları" : "Scrobbling settings";
                    else if (tb.Text == "Rich Presence display" || tb.Text == "Zengin Durum görünümü") tb.Text = isTurkish ? "Zengin Durum görünümü" : "Rich Presence display";
                    else if (tb.Text == "Max time before scrobble (sec)" || tb.Text == "Scrobble öncesi max süre (sn)") tb.Text = isTurkish ? "Scrobble öncesi max süre (sn)" : "Max time before scrobble (sec)";
                    else if (tb.Text == "User token" || tb.Text == "Kullanıcı tokeni") tb.Text = isTurkish ? "Kullanıcı tokeni" : "User token";
                    else if (tb.Text == "API Key" || tb.Text == "API Anahtarı") tb.Text = isTurkish ? "API Anahtarı" : "API Key";
                    else if (tb.Text == "API Secret" || tb.Text == "API Gizli Anahtarı") tb.Text = isTurkish ? "API Gizli Anahtarı" : "API Secret";
                    else if (tb.Text == "Username" || tb.Text == "Kullanıcı adı") tb.Text = isTurkish ? "Kullanıcı adı" : "Username";
                    else if (tb.Text == "Password" || tb.Text == "Şifre") tb.Text = isTurkish ? "Şifre" : "Password";
                }
                else if (child is StackPanel sp) {
                    foreach (var spChild in sp.Children) {
                        if (spChild is TextBlock spTb) {
                            if (spTb.Text == "Apple Music region" || spTb.Text == "Apple Music bölgesi") spTb.Text = isTurkish ? "Apple Music bölgesi" : "Apple Music region";
                            else if (spTb.Text == "Rich Presence display" || spTb.Text == "Zengin Durum görünümü") spTb.Text = isTurkish ? "Zengin Durum görünümü" : "Rich Presence display";
                            else if (spTb.Text == "Max time before scrobble (sec)" || spTb.Text == "Scrobble öncesi max süre (sn)") spTb.Text = isTurkish ? "Scrobble öncesi max süre (sn)" : "Max time before scrobble (sec)";
                        }
                    }
                }
            }

            CheckBox_EnableDiscordRP.Content = isTurkish ? "Discord RP'yi etkinleştir" : "Enable Discord RP";
            CheckBox_EnableRPCoverImages.Content = isTurkish ? "Kapak resimlerini göster" : "Enable cover images";
            CheckBox_EnableAlbumInfo.Content = isTurkish ? "Albüm bilgisini göster" : "Enable album info";
            CheckBox_ShowRPWhenMusicPaused.Content = isTurkish ? "Müzik duraklatıldığında RP'yi kapatma" : "RP when music paused";
            CheckBox_ShowAppleMusicIcon.Content = isTurkish ? "Durumda Apple Music ikonunu göster" : "Apple Music icon in status";

            int selectedIdx = ComboBox_RPDisplayChoice.SelectedIndex;
            ComboBox_RPDisplayChoice.Items.Clear();
            ComboBox_RPDisplayChoice.Items.Add(new ComboBoxItem { Content = isTurkish ? "Sanatçı Adı" : "Artist Name" });
            ComboBox_RPDisplayChoice.Items.Add(new ComboBoxItem { Content = isTurkish ? "Apple Music" : "Apple Music" });
            ComboBox_RPDisplayChoice.Items.Add(new ComboBoxItem { Content = isTurkish ? "Şarkı Adı" : "Song Name" });
            ComboBox_RPDisplayChoice.SelectedIndex = selectedIdx;

            CheckBox_EnableSyncLyrics.Content = isTurkish ? "Senkronize şarkı sözlerini etkinleştir" : "Enable sync lyrics";
            CheckBox_ExtendLyricsLine.Content = isTurkish ? "Söz satırını sonraki söze kadar uzat" : "Extend lyrics line";
            Button_OpenLyricCache.Content = isTurkish ? "Kaydedilen sözleri aç" : "Open saved lyrics";
            Button_DeleteLyricCache.Content = isTurkish ? "Kaydedilen sözleri sil" : "Delete saved lyrics";

            CheckBox_LastfmCleanAlbumName.Content = isTurkish ? "Albüm adını temizle" : "Clean album name";
            CheckBox_LastfmScrobblePrimary.Content = isTurkish ? "Sadece ana sanatçıyı scrobble et" : "Scrobble primary artist";
            CheckBox_ScrobblePreferAppleMusicWebDuration.Content = isTurkish ? "Şarkı süresini Apple Music Web'den almayı tercih et" : "Prefer song duration from Apple Music Web";
            CheckBox_LastfmEnable.Content = isTurkish ? "Last.FM'i etkinleştir" : "Enable Last.FM";
            CheckBox_ListenBrainzEnable.Content = isTurkish ? "ListenBrainz'i etkinleştir" : "Enable ListenBrainz";
            SaveLastFMCreds.Content = isTurkish ? "Kimlik Bilgilerini Kaydet" : "Save Credentials";
        }

        private void CheckBox_RunOnStartup_Click(object sender, RoutedEventArgs e) {
            if (CheckBox_RunOnStartup.IsChecked == true) {
                AddStartupShortcut();
            } else {
                RemoveStartupShortcut();
            }
            SaveSettings();
        }
        private void CheckBox_CheckForUpdatesOnStartup_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_ClassicalComposerAsArtist_Click(object sender, RoutedEventArgs e) {
            ((App)Application.Current).UpdateScraperPreferences(CheckBox_ClassicalComposerAsArtist.IsChecked == true);
            SaveSettings();
        }

        private void CheckBox_EnableRPCoverImages_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void AppleMusicRegion_TextChanged(object sender, TextChangedEventArgs e) {
            if (amRegionValid) {
                Application.Current.Resources.Remove("TextControlFocusedBorderBrush");
            } else {
                Application.Current.Resources["TextControlFocusedBorderBrush"] = new SolidColorBrush(Colors.Red);
                if (AppleMusicRegion.Text.Length > 2) {
                    AppleMusicRegion.Text = AppleMusicRegion.Text.Substring(0, 2);
                }
            }
            AppleMusicRegion.Text = AppleMusicRegion.Text.ToUpper();
            AppleMusicRegion.CaretIndex = Math.Max(0, AppleMusicRegion.Text.Length);

        }

        private void AppleMusicRegion_LostFocus(object sender, RoutedEventArgs e) {
            UpdateAppleMusicRegion();
        }

        private void AppleMusicRegion_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                UpdateAppleMusicRegion();
            }
        }

        private void CheckBox_EnableDiscordRP_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_EnableAlbumInfo_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_ShowRPWhenMusicPaused_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_ShowAppleMusicIcon_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void ComboBox_RPDisplayChoice_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var newOption = AppleMusicDiscordClient.PreviewOptionFromIndex(ComboBox_RPDisplayChoice.SelectedIndex);
            ((App)Application.Current).UpdateRPPreviewDisplay(newOption);
            SaveSettings();
        }


        private void CheckBox_EnableSyncLyrics_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_ExtendLyricsLine_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void Button_OpenLyricCache_Click(object sender, RoutedEventArgs e) {
            var path = Path.Combine(Constants.AppDataFolder, "LyricCache");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            Process.Start(new ProcessStartInfo {
                FileName = path,
                UseShellExecute = true
            });
        }

        private void Button_DeleteLyricCache_Click(object sender, RoutedEventArgs e) {
            bool isTurkish = Properties.Settings.Default.AppleMusicRegion.Equals("tr", StringComparison.OrdinalIgnoreCase);
            var path = Path.Combine(Constants.AppDataFolder, "LyricCache");
            if (Directory.Exists(path)) {
                var result = MessageBox.Show(
                    isTurkish ? "Tüm kaydedilen sözleri silmek istediğinize emin misiniz?" : "Are you sure you want to delete all saved lyrics?",
                    isTurkish ? "Kaydedilen Sözleri Sil" : "Delete Saved Lyrics", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes) {
                    try {
                        foreach (var file in Directory.GetFiles(path)) {
                            File.Delete(file);
                        }
                        MessageBox.Show(
                            isTurkish ? "Tüm kaydedilen sözler silindi." : "All saved lyrics have been deleted.", 
                            isTurkish ? "Sözler Silindi" : "Lyrics Deleted", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    } catch (Exception ex) {
                        MessageBox.Show(
                            (isTurkish ? "Sözler silinemedi: " : "Could not delete lyrics: ") + ex.Message, 
                            isTurkish ? "Hata" : "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            } else {
                MessageBox.Show(
                    isTurkish ? "Kaydedilen söz bulunamadı." : "No saved lyrics found.", 
                    isTurkish ? "Bilgi" : "Information", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CheckBox_LastfmEnable_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_ListenBrainzEnable_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_LastfmCleanAlbumName_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_LastfmScrobblePrimary_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_ScrobblePreferAppleMusicWebDuration_Checked(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private static void AddStartupShortcut() {
            // from https://stackoverflow.com/questions/234231/creating-application-shortcut-in-a-directory
            var t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t!)!;
            try {
                var lnk = shell.CreateShortcut(Constants.AppShortcutPath);
                try {
                    lnk.TargetPath = Constants.ExePath;
                    lnk.IconLocation = $"{Constants.ExePath}, 0";
                    lnk.Save();
                } finally {
                    Marshal.FinalReleaseComObject(lnk);
                }
            } finally {
                Marshal.FinalReleaseComObject(shell);
            }
        }

        private static void RemoveStartupShortcut() {
            File.Delete(Constants.AppShortcutPath);
        }

        private static void SaveSettings() {
            Properties.Settings.Default.Save();
        }

        private void UpdateAppleMusicRegion() {
            if (amRegionValid) {
                AppleMusicRegion.Resources.Remove("TextControlFocusedBorderBrush");
                Properties.Settings.Default.AppleMusicRegion = AppleMusicRegion.Text.ToLower();
            } else {
                AppleMusicRegion.Resources["TextControlFocusedBorderBrush"] = Brushes.Red;
                AppleMusicRegion.Text = Properties.Settings.Default.AppleMusicRegion;
            }
            SaveSettings();
            ((App)Application.Current).UpdateRegion();
            LocalizeUI();
        }
        private void ScrobbleMaxTime_TextChanged(object sender, TextChangedEventArgs e) {
            try {
                int.Parse(ScrobbleMaxTime.Text);
            } catch {
                ScrobbleMaxTime.Text = $"{Properties.Settings.Default.ScrobbleMaxWait}";
            } finally {
                SaveSettings();
            }
        }

        private async void SaveLastFMCreds_Click(object sender, RoutedEventArgs e) {
            // Store the actual password to the Credential Manager.  Not as good as true Last.FM tokenized authentication,
            //       but better than storing plain-text password in config file
            //       https://stackoverflow.com/questions/32548714/how-to-store-and-retrieve-credentials-on-windows-using-c-sharp
            try {
                CredentialManager.WriteCredential(
                    applicationName: Constants.LastFMCredentialTargetName,
                    userName: "",
                    secret: LastfmPassword.Password,
                    persistence: CredentialPersistence.LocalMachine);
            } catch (Exception ex) {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);

            }
            SaveSettings(); // The other three values are just stored in Settings

            if (Properties.Settings.Default.LastfmEnable) {
                // Signals the LastFM Scrobbler to re-init with new credentials
                var result = await ((App)Application.Current).UpdateLastfmCreds();
                bool isTurkish = Properties.Settings.Default.AppleMusicRegion.Equals("tr", StringComparison.OrdinalIgnoreCase);
                if (result) {
                    MessageBox.Show(
                        isTurkish ? "Last.FM kimlik bilgileri başarıyla doğrulandı." : "The Last.FM credentials were successfully authenticated.", 
                        isTurkish ? "Last.FM Doğrulaması" : "Last.FM Authentication", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show(
                        isTurkish ? "Last.FM kimlik bilgileri doğrulanamadı. Lütfen kullanıcı adı ve şifrenizi doğru girdiğinizden ve hesabınızın kilitli olmadığından emin olun." : "The Last.FM credentials could not be authenticated. Please make sure you have entered the correct username and password, and that your account is not currently locked.", 
                        isTurkish ? "Last.FM Doğrulaması" : "Last.FM Authentication", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (Properties.Settings.Default.ListenBrainzEnable) {
                // Signals the ListenBrainz Scrobbler to re-init with new credentials
                var result = await ((App)Application.Current).UpdateListenBrainzCreds();
                bool isTurkish = Properties.Settings.Default.AppleMusicRegion.Equals("tr", StringComparison.OrdinalIgnoreCase);
                if (result) {
                    MessageBox.Show(
                        isTurkish ? "ListenBrainz kimlik bilgileri başarıyla doğrulandı." : "The ListenBrainz credentials were successfully authenticated.", 
                        isTurkish ? "ListenBrainz Doğrulaması" : "ListenBrainz Authentication", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show(
                        isTurkish ? "ListenBrainz kimlik bilgileri doğrulanamadı. Lütfen kullanıcı tokeninizi doğru girdiğinizden emin olun." : "The ListenBrainz credentials could not be authenticated. Please make sure you have entered the correct user token.", 
                        isTurkish ? "ListenBrainz Doğrulaması" : "ListenBrainz Authentication", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            // Close();
        }

        public static string GetLastFMPassword() {
            // Read the stored password from Windows Credential Manager and use it to log into Last.FM
            var cred = CredentialManager.ReadCredential(applicationName: Constants.LastFMCredentialTargetName);
            return cred?.Password ?? String.Empty;
        }
    }
}
