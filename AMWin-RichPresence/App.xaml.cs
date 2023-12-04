using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using IF.Lastfm.Core.Api;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private TaskbarIcon? taskbarIcon;
        private AppleMusicClientScraper amScraper;
        private AppleMusicDiscordClient discordClient;
        private AppleMusicScrobbler scrobblerClient;
        private Logger logger;

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
        public App() {

            // make logger
            logger = new Logger();
            logger.Log("Application started");

            // start Discord RPC
            var subtitleOptions = (AppleMusicDiscordClient.RPSubtitleDisplayOptions)AMWin_RichPresence.Properties.Settings.Default.RPSubtitleChoice;
            var classicalComposerAsArtist = AMWin_RichPresence.Properties.Settings.Default.ClassicalComposerAsArtist;
            discordClient = new(Constants.DiscordClientID, enabled: false, subtitleOptions: subtitleOptions, logger: logger);

            // start Last.FM scrobbler
            scrobblerClient = new AppleMusicScrobbler();
            _ = scrobblerClient.init(lastFmCredentials);

            var lastFMApiKey = AMWin_RichPresence.Properties.Settings.Default.LastfmAPIKey;

            if (lastFMApiKey == null || lastFMApiKey == "") {
                logger.Log("No Last.FM API key found");
            }

            // start Apple Music scraper
            amScraper = new(lastFMApiKey, Constants.RefreshPeriod, classicalComposerAsArtist, (newInfo) => {
                
                // don't update scraper if Apple Music is paused or not open
                if (newInfo != null && newInfo != null && (AMWin_RichPresence.Properties.Settings.Default.ShowRPWhenMusicPaused || !newInfo.IsPaused)) {

                    // Discord RP update
                    if (AMWin_RichPresence.Properties.Settings.Default.EnableDiscordRP) {
                        discordClient.Enable();
                        discordClient.SetPresence(newInfo, AMWin_RichPresence.Properties.Settings.Default.ShowAppleMusicIcon, AMWin_RichPresence.Properties.Settings.Default.EnableRPCoverImages);
                    } else {
                        discordClient.Disable();
                    }

                    // Last.FM scrobble update
                    var scrobbler = scrobblerClient.GetLastFmScrobbler();
                    var trackApi = scrobblerClient.GetTrackApi();
                    if (AMWin_RichPresence.Properties.Settings.Default.LastfmEnable && scrobbler != null && trackApi != null) {
                        scrobblerClient.Scrobbleit(newInfo, scrobbler, trackApi);
                    }
                } else {
                    discordClient.Disable();
                }
            }, logger);
        }
        
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            taskbarIcon = (TaskbarIcon)FindResource("TaskbarIcon");
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            taskbarIcon?.Dispose();
            discordClient.Disable();
            logger.Log("Application finished");
        }

        internal void UpdateRPSubtitleDisplay(AppleMusicDiscordClient.RPSubtitleDisplayOptions newVal) {
            discordClient.subtitleOptions = newVal;
        }

        internal async Task<bool> UpdateLastfmCreds() {
            return await scrobblerClient.UpdateCredsAsync(lastFmCredentials);
        }
        internal void UpdateScraperPreferences(bool composerAsArtist) {
            amScraper.composerAsArtist = composerAsArtist;
        }
    }
}