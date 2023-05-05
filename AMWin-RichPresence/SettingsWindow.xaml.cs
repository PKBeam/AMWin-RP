using CredentialManagement;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace AMWin_RichPresence {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        public SettingsWindow() {
            InitializeComponent();
            TextBlock_VersionString.Text = Constants.ProgramVersion;
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
        private void CheckBox_EnableDiscordRP_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_ShowAppleMusicIcon_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void ComboBox_RPSubtitleChoice_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var newOption = AppleMusicDiscordClient.SubtitleOptionFromIndex(ComboBox_RPSubtitleChoice.SelectedIndex);
            ((App)Application.Current).UpdateRPSubtitleDisplay(newOption);

            SaveSettings();
        }

        private void CheckBox_LastfmEnable_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }
        private void CheckBox_LastfmCleanAlbumName_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void CheckBox_LastfmScrobblePrimary_Click(object sender, RoutedEventArgs e) {
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

        private void SaveLastFMCreds_Click(object sender, RoutedEventArgs e)
        {
            // Store the actual password to the Credential Manager.  Not as good as true Last.FM tokenized authentication,
            //       but better than storing plain-text password in config file
            //       https://stackoverflow.com/questions/32548714/how-to-store-and-retrieve-credentials-on-windows-using-c-sharp
            try
            {
                using (var cred = new SecureCredential())
                {

                    cred.SecurePassword = ConvertToSecureString(LastfmPassword.Password);
                    cred.Target = Constants.LastFMCredentialTargetName;
                    cred.Type = CredentialType.Generic;
                    cred.PersistanceType = PersistanceType.LocalComputer;
                    cred.Save();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);

            }
            SaveSettings(); // The other three values are just stored in Settings

            // Signals the LastFM Scrobbler to re-init with new credentials
            ((App)Application.Current).UpdateLastfmCreds(showMessageBoxOnSuccess: true);

           // Close();
        }

        private SecureString ConvertToSecureString(string password)
        {
            var securePassword = new SecureString();

            foreach (char c in password) securePassword.AppendChar(c);
            securePassword.MakeReadOnly();
            return securePassword;
        }

        public static string GetLastFMPassword()
        {
            // Read the stored password from Windows Credential Manager and use it to log into Last.FM
            try
            {
                using (var cred = new SecureCredential())
                {
                    cred.Target = Constants.LastFMCredentialTargetName;
                    cred.Load();
                    return ToPlainString(cred.SecurePassword);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
                return String.Empty;
            }
        }

        public static String ToPlainString(System.Security.SecureString secureStr)
        {
            String plainStr = new System.Net.NetworkCredential(string.Empty, secureStr).Password;
            return plainStr;
        }
    }
}
