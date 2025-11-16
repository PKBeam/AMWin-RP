using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AMWin_RichPresence {
    public partial class Taskbar {

        private SettingsWindow? settingsWindow;

        private void ShowWindow() {
            if (settingsWindow == null) {
                settingsWindow = new SettingsWindow();
            }
            settingsWindow!.Show(); 
            settingsWindow!.Focus();
        }

        internal void TaskbarIcon_DoubleClick(object sender, RoutedEventArgs e) {
            ShowWindow();
        }

        internal void MenuItemSettings_Click(object sender, RoutedEventArgs e) {
            ShowWindow();
        }

        internal void MenuItemExit_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }
    }
}
