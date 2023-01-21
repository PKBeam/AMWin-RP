using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private TaskbarIcon? taskbarIcon;
        private MainWindow mainWindow;
        private AppleMusicScraper amScraper;
        private AppleMusicDiscordClient discordClient;

        public App() {
            mainWindow = new MainWindow();

            // start Discord RPC
            discordClient = new(Constants.DiscordClientID, enabled: false);

            // start scraper
            amScraper = new(Constants.RefreshPeriod, (newInfo) => {
                // disable RPC when Apple Music is paused or not open
                if (newInfo != null && !((AppleMusicInfo)newInfo).IsPaused) {
                    discordClient.Enable();
                    discordClient.SetPresence((AppleMusicInfo)newInfo);
                } else {
                    discordClient.Disable();
                }
            });
        }
        
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            taskbarIcon = (TaskbarIcon)FindResource("TaskbarIcon");
            taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_DoubleClick;

            // bind commands to context menu
            var contextMenu = taskbarIcon.ContextMenu;
            BindContextMenuActions(contextMenu);
        }
        protected override void OnExit(ExitEventArgs e) {
            taskbarIcon?.Dispose();
            discordClient.Disable();
            base.OnExit(e);
        }
        private void TaskbarIcon_DoubleClick(object sender, RoutedEventArgs e) {
            //mainWindow.Show();
        }
        private void MenuItemSettings_Click(object sender, RoutedEventArgs e) {
            //mainWindow.Show();
        }
        private void MenuItemExit_Click(object sender, RoutedEventArgs e) {
            Current.Shutdown();
        }

        private void BindContextMenuActions(ContextMenu menu) {
            foreach (var item in menu.Items) {
                if (item is MenuItem) {
                    var menuItem = (MenuItem)item;
                    switch (menuItem.Header) {
                        case "Settings":
                            menuItem.Click += MenuItemSettings_Click;
                            break;
                        case "Exit":
                            menuItem.Click += MenuItemExit_Click;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
