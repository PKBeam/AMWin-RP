using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        public SettingsWindow() {
            InitializeComponent();
        }

        private void CheckBox_RunOnStartup_Click(object sender, RoutedEventArgs e) {
            if (CheckBox_RunOnStartup.IsChecked == true) {
                AddStartupShortcut();
            } else {
                RemoveStartupShortcut();
            }

            SaveSettings();
        }
        private void ComboBox_RPSubtitleChoice_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var newOption = AppleMusicDiscordClient.SubtitleOptionFromIndex(ComboBox_RPSubtitleChoice.SelectedIndex);
            ((App)Application.Current).UpdateRPSubtitleDisplay(newOption);

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
    }
}
