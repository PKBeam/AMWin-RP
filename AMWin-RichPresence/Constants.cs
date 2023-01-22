using System;
using System.Diagnostics;
using System.IO;

namespace AMWin_RichPresence {
    internal static class Constants {
        private static string ProgramVersionBase = "v0.2.0";
#if DEBUG
        public static string  ProgramVersion = $"{ProgramVersionBase}-dev";
#else
        public static string  ProgramVersion = ProgramVersionBase;
#endif                        
        public static int     RefreshPeriod = 5; // seconds
        public static string  DiscordClientID = "1066220978406953012";
        public static string  DiscordAppleMusicImageKey = "applemusic1024x";
        public static string  DiscordAppleMusicPlayImageKey = "applemusicplay1024x";
        public static string WindowsStartupFolder => Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        public static string AppShortcutPath => Path.Join(WindowsStartupFolder, "AMWin-RP.lnk");
        public static string? ExePath => Process.GetCurrentProcess().MainModule?.FileName;         
    }
}