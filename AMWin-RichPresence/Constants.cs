using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace AMWin_RichPresence {
    internal static class Constants {
        private static string ProgramVersionBase {
            get {
                try {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (exePath == null) {
                        return "";
                    }
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exePath);
                    return $"v{fvi.FileVersion}";
                } catch (Exception ex) {
                    new Logger().Log($"Error getting version string: {ex}");
                    return "";
                }
            }
        } 
#if DEBUG
        public static string  ProgramVersion = $"{ProgramVersionBase}-dev";
#else
        public static string  ProgramVersion = ProgramVersionBase;
#endif                        
        public static int    MaxLogFiles                    = 10;
        public static int    RefreshPeriod                  = 5; // seconds
        public static string AppDataFolderName              = "AMWin-RichPresence";
        public static string DiscordClientID                = "1066220978406953012";
        public static string DiscordAppleMusicImageKey      = "applemusic1024x";
        public static string DiscordAppleMusicPlayImageKey  = "applemusicplay1024x";
        public static string DiscordAppleMusicPauseImageKey = "applemusicpause1024x";
        public static string LastFMCredentialTargetName     = "Last FM Password";
        public static int    LastFMTimeBeforeScrobbling     = 20; // seconds

        public static string WindowsStartupFolder => Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        public static string WindowsAppDataFolder => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static string AppDataFolder => Path.Combine(WindowsAppDataFolder, AppDataFolderName);
        public static string AppShortcutPath => Path.Join(WindowsStartupFolder, "AMWin-RP.lnk");
        public static string? ExePath => Process.GetCurrentProcess().MainModule?.FileName;

        public static readonly HttpClient HttpClient = new HttpClient();
    }
}