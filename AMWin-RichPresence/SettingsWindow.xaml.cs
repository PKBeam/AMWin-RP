using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Meziantou.Framework.Win32;
using Localisation = AMWin_RichPresence.Properties.Localisation;

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
                    Localisation.Message_ClearLyricCache,
                    Localisation.Message_ClearLyricCache_Title, 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes) {
                    try {
                        foreach (var file in Directory.GetFiles(path)) {
                            File.Delete(file);
                        }
                        MessageBox.Show(
                            Localisation.Message_ClearedLyricCache,
                            Localisation.Message_ClearedLyricCache_Title,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    } catch (Exception ex) {
                        MessageBox.Show(
                            Localisation.Message_ClearLyricCache_Fail + ex.Message,
                            Localisation.Message_Error,
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            } else {
                MessageBox.Show(
                    Localisation.Message_ClearLyricCache_FailNotFound,
                    Localisation.Message_Information,
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
                        Localisation.Message_LastFM_Authentication_Success,
                        Localisation.Message_LastFM_Authentication_Title,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show(
                        Localisation.Message_LastFM_Authentication_Fail,
                        Localisation.Message_LastFM_Authentication_Title,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (Properties.Settings.Default.ListenBrainzEnable) {
                // Signals the ListenBrainz Scrobbler to re-init with new credentials
                var result = await ((App)Application.Current).UpdateListenBrainzCreds();
                if (result) {
                    MessageBox.Show(
                        Localisation.Message_ListenBrainz_Authentication_Success,
                        Localisation.Message_ListenBrainz_Authentication_Title,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show(
                        Localisation.Message_LastFM_Authentication_Fail,
                        Localisation.Message_LastFM_Authentication_Title,
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
