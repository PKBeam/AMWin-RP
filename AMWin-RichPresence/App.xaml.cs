using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private TaskbarIcon? taskbarIcon;
        private AppleMusicClientScraper amScraper;
        private AppleMusicDiscordClient discordClient;
        private AppleMusicScrobbler scrobblerClient;
        public App() {

            // start Discord RPC
            var subtitleOptions = (AppleMusicDiscordClient.RPSubtitleDisplayOptions)AMWin_RichPresence.Properties.Settings.Default.RPSubtitleChoice;
            discordClient = new(Constants.DiscordClientID, enabled: false, subtitleOptions: subtitleOptions);

            // start Last.FM scrobbler
            scrobblerClient = new AppleMusicScrobbler();
            scrobblerClient.init();

            // start Apple Music scraper
            amScraper = new(Constants.RefreshPeriod, (newInfo) => {
                bool userEnabledRP = AMWin_RichPresence.Properties.Settings.Default.EnableDiscordRP;
                // disable RPC when requested by the user, and also when Apple Music is paused/not open
                if (userEnabledRP && newInfo != null && newInfo != null && !newInfo.IsPaused) {
                    discordClient.Enable();
                    discordClient.SetPresence(newInfo, AMWin_RichPresence.Properties.Settings.Default.ShowAppleMusicIcon);
                    scrobblerClient.Scrobbleit(newInfo, scrobblerClient.GetLastFmScrobbler());
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
            scrobblerClient.UpdateCreds(showMessageBoxOnSuccess);
        }
    }
}
