using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Localisation = AMWin_RichPresence.Properties.Localisation;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private static readonly CultureInfo InitialUICulture = CultureInfo.CurrentUICulture;
        private const string OpenSettingsWindowArg = "--open-settings-window";

        private TaskbarIcon? taskbarIcon;
        private AppleMusicClientScraper amScraper;
        private AppleMusicDiscordClient discordClient;
        private AppleMusicLastFmScrobbler lastFmScrobblerClient;
        private AppleMusicListenBrainzScrobbler listenBrainzScrobblerClient;
        private Logger? logger;

        internal static string NormalizeLanguageCode(string? languageCode) {
            return languageCode?.Trim().ToLowerInvariant() switch {
                "en" => "en",
                "tr" => "tr",
                "ko" => "ko",
                "ja" => "ja",
                _ => ""
            };
        }

        internal static void ApplyLanguagePreference() {
            var languageCode = NormalizeLanguageCode(AMWin_RichPresence.Properties.Settings.Default.Language);
            var culture = languageCode == ""
                ? InitialUICulture
                : CultureInfo.GetCultureInfo(languageCode);

            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Localisation.Culture = culture;
        }

        internal static void RestartApplication(bool openSettingsWindow = false) {
            var exePath = Constants.ExePath;
            if (!string.IsNullOrWhiteSpace(exePath)) {
                Process.Start(new ProcessStartInfo {
                    FileName = exePath,
                    Arguments = openSettingsWindow ? OpenSettingsWindowArg : "",
                    UseShellExecute = true
                });
            }
            Application.Current.Shutdown();
        }

        public LastFmCredentials lastFmCredentials {
            get {
                var creds = new LastFmCredentials();
                creds.apiKey = AMWin_RichPresence.Properties.Settings.Default.LastfmAPIKey;
                creds.apiSecret = AMWin_RichPresence.Properties.Settings.Default.LastfmSecret;
                creds.username = AMWin_RichPresence.Properties.Settings.Default.LastfmUsername;
                creds.password = SettingsWindow.GetLastFMPassword();
                return creds;
            }
        }

        public ListenBrainzCredentials listenBrainzCredentials {
            get {
                var creds = new ListenBrainzCredentials();
                creds.userToken = AMWin_RichPresence.Properties.Settings.Default.ListenBrainzUserToken;
                return creds;
            }
        }

        public App() {
            ApplyLanguagePreference();

            // make logger
            try {
                logger = new Logger();
                logger.Log("Application started");
                logger.Log($"{Environment.OSVersion}");
                logger.Log($"Using UI language: {CultureInfo.CurrentUICulture.Name}");
            } catch {
                logger = null;
            }

            // try to auto detect region
            if (AMWin_RichPresence.Properties.Settings.Default.AppleMusicRegion == "") {
                var region = RegionInfo.CurrentRegion.Name.ToLower();
                if (Constants.ValidAppleMusicRegions.Contains(region)) {
                    AMWin_RichPresence.Properties.Settings.Default.AppleMusicRegion = region;
                } else {
                    AMWin_RichPresence.Properties.Settings.Default.AppleMusicRegion = Constants.DefaultAppleMusicRegion;
                }
                AMWin_RichPresence.Properties.Settings.Default.Save();
            }
            logger?.Log($"Using region {AMWin_RichPresence.Properties.Settings.Default.AppleMusicRegion}");

            // check for updates
            if (AMWin_RichPresence.Properties.Settings.Default.CheckForUpdatesOnStartup) {
                try {
                    CheckForUpdates();
                    logger?.Log("No AMWin-RP updates available.");
                } catch (Exception e) {
                    logger?.Log($"Could not check for AMWin-RP updates: {e.Message}");
                }
            }

            // start Discord RPC
            var statusDisplayOptions = (AppleMusicDiscordClient.RPStatusDisplayOptions)AMWin_RichPresence.Properties.Settings.Default.RPDisplayChoice;
            var classicalComposerAsArtist = AMWin_RichPresence.Properties.Settings.Default.ClassicalComposerAsArtist;
            discordClient = new(Constants.DiscordClientID, enabled: false, statusDisplayOptions: statusDisplayOptions, logger: logger);

            // start Last.FM scrobbler
            var amRegion = AMWin_RichPresence.Properties.Settings.Default.AppleMusicRegion;
            lastFmScrobblerClient = new AppleMusicLastFmScrobbler(region: amRegion, logger: logger);
            _ = lastFmScrobblerClient.init(lastFmCredentials);

            var lastFMApiKey = AMWin_RichPresence.Properties.Settings.Default.LastfmAPIKey;
            if (lastFMApiKey == null || lastFMApiKey == "") {
                logger?.Log("No Last.FM API key found");
            }

            // start ListenBrainz scrobbler
            listenBrainzScrobblerClient = new AppleMusicListenBrainzScrobbler(region: amRegion, logger: logger);
            _ = listenBrainzScrobblerClient.init(listenBrainzCredentials);

            // start Apple Music scraper
            amScraper = new(lastFMApiKey, Constants.RefreshPeriod, classicalComposerAsArtist, AMWin_RichPresence.Properties.Settings.Default.AppleMusicRegion, (newInfo) => {

                // don't update scraper if Apple Music is paused or not open
                if (newInfo != null && (AMWin_RichPresence.Properties.Settings.Default.ShowRPWhenMusicPaused || !newInfo.IsPaused)) {

                    // Discord RP update
                    if (AMWin_RichPresence.Properties.Settings.Default.EnableDiscordRP) {
                        discordClient.Enable();
                        discordClient.SetPresence(newInfo, AMWin_RichPresence.Properties.Settings.Default.ShowAppleMusicIcon, AMWin_RichPresence.Properties.Settings.Default.EnableRPCoverImages);
                    } else {
                        discordClient.Disable();
                    }

                    // Last.FM scrobble update
                    if (AMWin_RichPresence.Properties.Settings.Default.LastfmEnable) {
                        lastFmScrobblerClient.Scrobbleit(newInfo);
                    }

                    // ListenBrainz scrobble update
                    if (AMWin_RichPresence.Properties.Settings.Default.ListenBrainzEnable) {
                        listenBrainzScrobblerClient.Scrobbleit(newInfo);
                    }
                } else {
                    discordClient.Disable();
                }
            }, logger);
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            taskbarIcon = (TaskbarIcon)FindResource("TaskbarIcon");
            if (e.Args.Contains(OpenSettingsWindowArg, StringComparer.OrdinalIgnoreCase)) {
                Current.Dispatcher.BeginInvoke(new Action(() => {
                    var settingsWindow = new SettingsWindow();
                    settingsWindow.Show();
                    settingsWindow.Focus();
                }));
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            taskbarIcon?.Dispose();
            discordClient.Disable();
            logger?.Log("Application finished");
        }

        internal void UpdateRPStatusDisplay(AppleMusicDiscordClient.RPStatusDisplayOptions newVal) {
            discordClient.statusDisplayOptions = newVal;
        }

        internal async Task<bool> UpdateLastfmCreds() {
            return await lastFmScrobblerClient.UpdateCredsAsync(lastFmCredentials);
        }

        internal async Task<bool> UpdateListenBrainzCreds() {
            return await listenBrainzScrobblerClient.UpdateCredsAsync(listenBrainzCredentials);
        }

        internal void UpdateRegion() {
            var region = AMWin_RichPresence.Properties.Settings.Default.AppleMusicRegion;
            logger?.Log($"Changed region to {region}");
            amScraper.ChangeRegion(region);
        }

        internal void UpdateScraperPreferences(bool composerAsArtist) {
            amScraper.composerAsArtist = composerAsArtist;
        }

        internal async void CheckForUpdates() {
            static int StringVerToInt(string v) {
                var verStr = v[1..].Split("b")[0].Replace(".", "").PadRight(4, '0');
                return int.Parse(verStr);
            }
            Constants.HttpClient.DefaultRequestHeaders.Add("User-Agent", "AMWin-RP");
            var result = await Constants.HttpClient.GetStringAsync(Constants.GithubReleasesApiUrl);
            var json = JsonDocument.Parse(result);

            var verLocal = Constants.ProgramVersionBase;
            var verRemote = json.RootElement.GetProperty("name").GetString()!;

            var numverLocal = StringVerToInt(verLocal);
            var numverRemote = StringVerToInt(verRemote);

            // TODO add support for multiple beta versions (i.e. b1 and b2)
            if (numverRemote > numverLocal || (numverRemote == numverLocal && verLocal.Contains('b') && !verRemote.Contains('b'))) {
                Application.Current.Dispatcher.Invoke((Action)async delegate {
                    var result = await new MessageBox {
                        Title = Localisation.Message_AppUpdate_Title,
                        Content = Localisation.Message_AppUpdate,
                        IsCloseButtonEnabled = false,
                        PrimaryButtonText = Localisation.Message_Yes,
                        SecondaryButtonText = Localisation.Message_No
                    }.ShowDialogAsync();

                    if (result == MessageBoxResult.Primary) {
                        Process.Start(new ProcessStartInfo {
                            FileName = Constants.GithubReleasesUrl,
                            UseShellExecute = true
                        });
                    }
                });
            }
        }
    }
}
