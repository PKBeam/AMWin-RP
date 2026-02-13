using Meziantou.Framework.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Localisation = AMWin_RichPresence.Properties.Localisation;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : FluentWindow {
        private bool isLanguageSelectorInitialized;

        private bool amRegionValid {
            get { return Constants.ValidAppleMusicRegions.Contains(AppleMusicRegion.Text.ToLower()); }
        }

        public SettingsWindow() {
            ApplicationThemeManager.ApplySystemTheme();
            SystemThemeWatcher.Watch(this);
            InitializeComponent();
            InitializeLanguageSelector();

            string imagePath = IsDarkMode()
                ? "/Resources/GitHub_Invertocat_White.png"
                : "/Resources/GitHub_Invertocat_Black.png";

            Image_GitHub.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            AppleMusicRegion.Text = Properties.Settings.Default.AppleMusicRegion.ToUpper();
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

        private void InitializeLanguageSelector() {
            TextBlock_LanguageLabel.Text = GetLocalisedString("Settings_General_Language", "Language");
            TextBlock_LanguageDescription.Text = GetLocalisedString("Settings_General_Language_Description", "Restart the app after changing the language.");
            ComboBoxItem_LanguageSystem.Content = GetLocalisedString("Settings_General_Language_System", "System default");
            ComboBoxItem_LanguageEnglish.Content = GetLocalisedString("Settings_General_Language_English", "English");
            ComboBoxItem_LanguageTurkish.Content = GetLocalisedString("Settings_General_Language_Turkish", "Turkce");
            ComboBoxItem_LanguageKorean.Content = GetLocalisedString("Settings_General_Language_Korean", "Korean");

            var selectedLanguage = App.NormalizeLanguageCode(Properties.Settings.Default.Language);
            if (!String.Equals(selectedLanguage, Properties.Settings.Default.Language, StringComparison.Ordinal)) {
                Properties.Settings.Default.Language = selectedLanguage;
                SaveSettings();
            }

            ComboBox_Language.SelectedItem = selectedLanguage switch {
                "en" => ComboBoxItem_LanguageEnglish,
                "tr" => ComboBoxItem_LanguageTurkish,
                "ko" => ComboBoxItem_LanguageKorean,
                _ => ComboBoxItem_LanguageSystem
            };

            isLanguageSelectorInitialized = true;
        }

        private static string GetLocalisedString(string key, string fallback) {
            return Localisation.ResourceManager.GetString(key, Localisation.Culture) ?? fallback;
        }

        private async void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!isLanguageSelectorInitialized) {
                return;
            }

            if (ComboBox_Language.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag is not string selectedLanguage) {
                return;
            }

            var normalizedLanguage = App.NormalizeLanguageCode(selectedLanguage);
            if (normalizedLanguage == App.NormalizeLanguageCode(Properties.Settings.Default.Language)) {
                return;
            }

            Properties.Settings.Default.Language = normalizedLanguage;
            SaveSettings();
            App.ApplyLanguagePreference();

            var result = await new MessageBox {
                Title = GetLocalisedString("Message_RestartRequired_Title", "Restart Required"),
                Content = GetLocalisedString("Message_RestartRequired_Content", "Restart now to apply the language change?"),
                IsCloseButtonEnabled = false,
                PrimaryButtonText = Localisation.Message_Yes,
                SecondaryButtonText = Localisation.Message_No,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            }.ShowDialogAsync();

            if (result == MessageBoxResult.Primary) {
                RestartApplication();
            }
        }

        private void AppleMusicRegion_TextChanged(object sender, TextChangedEventArgs e) {
            AppleMusicRegion.CharacterCasing = CharacterCasing.Upper;
            AppleMusicRegionStatusIcon.Visibility = amRegionValid ? Visibility.Hidden : Visibility.Visible;
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

        private async void Button_DeleteLyricCache_Click(object sender, RoutedEventArgs e) {
            var path = Path.Combine(Constants.AppDataFolder, "LyricCache");
            if (Directory.Exists(path)) {
                var result = await new MessageBox {
                    Title = Localisation.Message_ClearLyricCache,
                    Content = Localisation.Message_ClearLyricCache,
                    IsCloseButtonEnabled = false,
                    PrimaryButtonText = Localisation.Message_Yes,
                    SecondaryButtonText = Localisation.Message_No,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                }.ShowDialogAsync();

                if (result == MessageBoxResult.Primary) {
                    try {
                        foreach (var file in Directory.GetFiles(path)) {
                            File.Delete(file);
                        }
                        await new MessageBox {
                            Title = Localisation.Message_ClearedLyricCache_Title,
                            Content = Localisation.Message_ClearedLyricCache,
                            IsPrimaryButtonEnabled = false,
                            IsSecondaryButtonEnabled = false,
                            Owner = this,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        }.ShowDialogAsync();
                    } catch (Exception ex) {
                        await new MessageBox {
                            Title = Localisation.Message_Error,
                            Content = Localisation.Message_ClearLyricCache_Fail + ex.Message,
                            IsPrimaryButtonEnabled = false,
                            IsSecondaryButtonEnabled = false,
                            Owner = this,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        }.ShowDialogAsync();
                    }
                }
            } else {
                await new MessageBox {
                    Title = Localisation.Message_Information,
                    Content = Localisation.Message_ClearLyricCache_FailNotFound,
                    IsPrimaryButtonEnabled = false,
                    IsSecondaryButtonEnabled = false,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                }.ShowDialogAsync();
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

        private static void RestartApplication() {
            var exePath = Constants.ExePath;
            if (!string.IsNullOrWhiteSpace(exePath)) {
                Process.Start(new ProcessStartInfo {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
            Application.Current.Shutdown();
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
                    await new MessageBox {
                        Title = Localisation.Message_LastFM_Authentication_Title,
                        Content = Localisation.Message_LastFM_Authentication_Success,
                        IsPrimaryButtonEnabled = false,
                        IsSecondaryButtonEnabled = false,
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    }.ShowDialogAsync();
                } else {
                    await new MessageBox {
                        Title = Localisation.Message_LastFM_Authentication_Title,
                        Content = Localisation.Message_LastFM_Authentication_Fail,
                        IsPrimaryButtonEnabled = false,
                        IsSecondaryButtonEnabled = false,
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    }.ShowDialogAsync();
                }
            }
        }

        private async void SaveListenBrainzCreds_Click(object sender, RoutedEventArgs e) {
            if (Properties.Settings.Default.ListenBrainzEnable) {
                // Signals the ListenBrainz Scrobbler to re-init with new credentials
                var result = await ((App)Application.Current).UpdateListenBrainzCreds();
                if (result) {
                    await new MessageBox {
                        Title = Localisation.Message_ListenBrainz_Authentication_Title,
                        Content = Localisation.Message_ListenBrainz_Authentication_Success,
                        IsPrimaryButtonEnabled = false,
                        IsSecondaryButtonEnabled = false,
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    }.ShowDialogAsync();
                } else {
                    await new MessageBox {
                        Title = Localisation.Message_ListenBrainz_Authentication_Title,
                        Content = Localisation.Message_ListenBrainz_Authentication_Fail,
                        IsPrimaryButtonEnabled = false,
                        IsSecondaryButtonEnabled = false,
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    }.ShowDialogAsync();
                }
            }
        }

        public static string GetLastFMPassword() {
            // Read the stored password from Windows Credential Manager and use it to log into Last.FM
            var cred = CredentialManager.ReadCredential(applicationName: Constants.LastFMCredentialTargetName);
            return cred?.Password ?? String.Empty;
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e) {
            Process.Start(new ProcessStartInfo {
                FileName = Constants.GithubRepoUrl,
                UseShellExecute = true
            });
        }

        private bool IsDarkMode() {
            var lightTheme = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1);

            return lightTheme is int value && value == 0;
        }

        private void NavItemGeneral_Click(object sender, RoutedEventArgs e) {
            ScrollViewerSettings.ScrollToVerticalOffset(
                SectionTitleGeneral.TranslatePoint(new Point(0, 0), ScrollViewerTop).Y
            );
        }

        private void NavItemDiscord_Click(object sender, RoutedEventArgs e) {
            ScrollViewerSettings.ScrollToVerticalOffset(
                SectionTitleDiscord.TranslatePoint(new Point(0, 0), ScrollViewerTop).Y
            );
        }

        private void NavItemScrobbling_Click(object sender, RoutedEventArgs e) {
            ScrollViewerSettings.ScrollToVerticalOffset(
                SectionTitleScrobbling.TranslatePoint(new Point(0, 0), ScrollViewerTop).Y
            );
        }
    }
}
