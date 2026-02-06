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
            bool isTurkish = Localization.CurrentRegion == "tr";
            
            this.Title = isTurkish ? "AMWin-RichPresence Ayarlar" : "AMWin-RichPresence Settings";

            CheckBox_RunOnStartup.Content = Localization.Get("Run when Windows starts");
            CheckBox_CheckForUpdatesOnStartup.Content = Localization.Get("Check for updates on startup");
            CheckBox_ClassicalComposerAsArtist.Content = Localization.Get("Treat composer as artist");
            
            // Find and translate TextBlocks in the main stack
            var scrollViewer = (ScrollViewer)((Border)((DockPanel)((Grid)this.Content).Children[0]).Children[2]).Child;
            var mainStack = (StackPanel)scrollViewer.Content;
            
            foreach (var child in mainStack.Children) {
                if (child is TextBlock tb) {
                    // Try to translate the text if it matches a known key
                    string translated = Localization.Get(tb.Text);
                    if (translated != tb.Text) tb.Text = translated;
                    
                    // Specific fallback check for already translated items to allow switching back to English
                    if (!isTurkish) {
                        if (tb.Text == "Discord ayarları") tb.Text = "Discord settings";
                        else if (tb.Text == "Şarkı sözü ayarları") tb.Text = "Lyrics settings";
                        else if (tb.Text == "Scrobbling ayarları") tb.Text = "Scrobbling settings";
                    }
                }
                else if (child is StackPanel sp) {
                    foreach (var spChild in sp.Children) {
                        if (spChild is TextBlock spTb) {
                            string translated = Localization.Get(spTb.Text);
                            if (translated != spTb.Text) spTb.Text = translated;
                        }
                    }
                }
            }

            CheckBox_EnableDiscordRP.Content = Localization.Get("Enable Discord RP");
            CheckBox_EnableRPCoverImages.Content = Localization.Get("Enable cover images");
            CheckBox_ShowRPWhenMusicPaused.Content = Localization.Get("RP when music paused");
            CheckBox_ShowAppleMusicIcon.Content = Localization.Get("Apple Music icon in status");

            int selectedIdx = ComboBox_RPDisplayChoice.SelectedIndex;
            ComboBox_RPDisplayChoice.Items.Clear();
            ComboBox_RPDisplayChoice.Items.Add(new ComboBoxItem { Content = Localization.Get("Artist Name") });
            ComboBox_RPDisplayChoice.Items.Add(new ComboBoxItem { Content = Localization.Get("Apple Music") });
            ComboBox_RPDisplayChoice.Items.Add(new ComboBoxItem { Content = Localization.Get("Song Name") });
            ComboBox_RPDisplayChoice.SelectedIndex = selectedIdx;

            CheckBox_EnableSyncLyrics.Content = Localization.Get("Enable sync lyrics");
            CheckBox_ExtendLyricsLine.Content = Localization.Get("Extend lyrics line");
            Button_OpenLyricCache.Content = Localization.Get("Open saved lyrics");
            Button_DeleteLyricCache.Content = Localization.Get("Delete saved lyrics");

            CheckBox_LastfmCleanAlbumName.Content = Localization.Get("Clean album name");
            CheckBox_LastfmScrobblePrimary.Content = Localization.Get("Scrobble primary artist");
            CheckBox_ScrobblePreferAppleMusicWebDuration.Content = Localization.Get("Prefer song duration from Apple Music Web");
            CheckBox_LastfmEnable.Content = Localization.Get("Enable Last.FM");
            CheckBox_ListenBrainzEnable.Content = Localization.Get("Enable ListenBrainz");
            SaveLastFMCreds.Content = Localization.Get("Save Credentials");
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

        private void CheckBox_ShowRPWhenMusicPaused_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_ShowAppleMusicIcon_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void ComboBox_RPDisplayChoice_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var newOption = AppleMusicDiscordClient.StatusDisplayOptionFromIndex(ComboBox_RPDisplayChoice.SelectedIndex);
            ((App)Application.Current).UpdateRPStatusDisplay(newOption);
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
            var path = Path.Combine(Constants.AppDataFolder, "LyricCache");
            if (Directory.Exists(path)) {
                var result = MessageBox.Show(
                    Localization.Get("Are you sure you want to delete all saved lyrics?"),
                    Localization.Get("Delete Saved Lyrics"), 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes) {
                    try {
                        foreach (var file in Directory.GetFiles(path)) {
                            File.Delete(file);
                        }
                        MessageBox.Show(
                            Localization.Get("All saved lyrics have been deleted."), 
                            Localization.Get("Lyrics Deleted"), 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    } catch (Exception ex) {
                        MessageBox.Show(
                            Localization.Get("Could not delete lyrics: ") + ex.Message, 
                            Localization.Get("Error"), 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            } else {
                MessageBox.Show(
                    Localization.Get("No saved lyrics found."), 
                    Localization.Get("Information"), 
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
                if (result) {
                    MessageBox.Show(
                        Localization.Get("The Last.FM credentials were successfully authenticated."), 
                        Localization.Get("Last.FM Authentication"), 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show(
                        Localization.Get("The Last.FM credentials could not be authenticated. Please make sure you have entered the correct username and password, and that your account is not currently locked."), 
                        Localization.Get("Last.FM Authentication"), 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (Properties.Settings.Default.ListenBrainzEnable) {
                // Signals the ListenBrainz Scrobbler to re-init with new credentials
                var result = await ((App)Application.Current).UpdateListenBrainzCreds();
                if (result) {
                    MessageBox.Show(
                        Localization.Get("The ListenBrainz credentials were successfully authenticated."), 
                        Localization.Get("ListenBrainz Authentication"), 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show(
                        Localization.Get("The ListenBrainz credentials could not be authenticated. Please make sure you have entered the correct user token."), 
                        Localization.Get("ListenBrainz Authentication"), 
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
