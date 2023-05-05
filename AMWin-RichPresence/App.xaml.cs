using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private TaskbarIcon? taskbarIcon;
        private AppleMusicClientScraper amScraper;
        private AppleMusicDiscordClient discordClient;
        private AppleMusicScrobbler scrobblerClient;
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

            // start Discord RPC
            var subtitleOptions = (AppleMusicDiscordClient.RPSubtitleDisplayOptions)AMWin_RichPresence.Properties.Settings.Default.RPSubtitleChoice;
            discordClient = new(Constants.DiscordClientID, enabled: false, subtitleOptions: subtitleOptions);

            // start Last.FM scrobbler
            scrobblerClient = new AppleMusicScrobbler();
            scrobblerClient.init(lastFmCredentials);

            // start Apple Music scraper
            amScraper = new(Constants.RefreshPeriod, (newInfo) => {
                
                // don't update scraper if Apple Music is paused or not open
                if (newInfo != null && newInfo != null && !newInfo.IsPaused) {

                    // Discord RP update
                    if (AMWin_RichPresence.Properties.Settings.Default.EnableDiscordRP) {
                        discordClient.Enable();
                        discordClient.SetPresence(newInfo, AMWin_RichPresence.Properties.Settings.Default.ShowAppleMusicIcon);
                    } else {
                        discordClient.Disable();
                    }

                    // Last.FM scrobble update
                    var scrobbler = scrobblerClient.GetLastFmScrobbler();
                    if (AMWin_RichPresence.Properties.Settings.Default.LastfmEnable && scrobbler != null) {
                        scrobblerClient.Scrobbleit(newInfo, scrobbler);
                    }
                } else {
                    discordClient.Disable();
                }
            });
        }
        
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            taskbarIcon = (TaskbarIcon)FindResource("TaskbarIcon");
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            taskbarIcon?.Dispose();
            discordClient.Disable();
        }

        internal void UpdateRPSubtitleDisplay(AppleMusicDiscordClient.RPSubtitleDisplayOptions newVal) {
            discordClient.subtitleOptions = newVal;
        }

        internal void UpdateLastfmCreds(bool showMessageBoxOnSuccess) {
            scrobblerClient.UpdateCreds(lastFmCredentials, showMessageBoxOnSuccess);
        }
    }
}
